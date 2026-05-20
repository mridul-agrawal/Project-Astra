using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Pathfinding
{
    // Stateless Dijkstra pathfinding on the tactical grid. All methods take
    // terrain and occupancy lookups as delegates, so the algorithm runs without
    // Unity and is straightforward to unit-test.
    public static class Pathfinder
    {
        public enum OccupantType { None, Ally, Enemy }

        // E, W, N, S — horizontal first so equal-cost paths break ties toward
        // horizontal travel, which reads more naturally on the displayed arrow.
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        // Output of a flood-fill. Destinations vs PassThrough are kept separate
        // so the UI can render them differently — Destinations are valid stopping
        // points; PassThrough is ally-occupied (walk through, can't stop on).
        public readonly struct ReachabilityResult
        {
            public readonly HashSet<Vector2Int> Destinations;
            public readonly HashSet<Vector2Int> PassThrough;
            public readonly Dictionary<Vector2Int, int> CostMap;
            public readonly Dictionary<Vector2Int, Vector2Int> Predecessors;

            public ReachabilityResult(
                HashSet<Vector2Int> destinations,
                HashSet<Vector2Int> passThrough,
                Dictionary<Vector2Int, int> costMap,
                Dictionary<Vector2Int, Vector2Int> predecessors)
            {
                Destinations = destinations;
                PassThrough = passThrough;
                CostMap = costMap;
                Predecessors = predecessors;
            }
        }

        // Movement cost for this terrain × movement type. 0 means impassable.
        public static int GetMovementCost(TerrainStats stats, MovementType moveType)
        {
            return moveType switch
            {
                MovementType.Foot => stats.moveCostFoot,
                MovementType.Mounted => stats.moveCostMounted,
                MovementType.Armoured => stats.moveCostArmoured,
                MovementType.Flying => stats.moveCostFlying,
                MovementType.Pirate => stats.moveCostPirate,
                MovementType.Thief => stats.moveCostThief,
                _ => 0
            };
        }

        // Dijkstra flood-fill from origin, returning every tile the unit can
        // reach within its movement budget. Ally tiles end up in PassThrough
        // (legal to cross, illegal to stop on); enemies block completely.
        public static ReachabilityResult ComputeReachability(
            Vector2Int origin,
            int movementPoints,
            MovementType moveType,
            int mapWidth,
            int mapHeight,
            Func<int, int, TerrainType> getTerrainType,
            Func<TerrainType, TerrainStats> getTerrainStats,
            Func<Vector2Int, OccupantType> getOccupant)
        {
            var search = NewSearch(origin);

            while (search.Frontier.Count > 0)
            {
                var (cost, current) = PopMinCost(search.Frontier);
                if (IsStaleFrontierEntry(cost, current, search.CostMap)) continue;

                ExpandNeighbors(current, cost, movementPoints, moveType,
                    mapWidth, mapHeight,
                    getTerrainType, getTerrainStats, getOccupant,
                    search);
            }

            return new ReachabilityResult(
                search.Destinations, search.PassThrough,
                search.CostMap, search.Predecessors);
        }

        // Walks the predecessor chain from destination back to origin. Returns
        // null if destination wasn't reached or isn't a legal stop.
        public static List<Vector2Int> ReconstructPath(
            Vector2Int origin,
            Vector2Int destination,
            ReachabilityResult result)
        {
            if (!result.CostMap.ContainsKey(destination)) return null;
            if (!result.Destinations.Contains(destination)) return null;

            var path = new List<Vector2Int>();
            Vector2Int current = destination;

            while (current != origin)
            {
                path.Add(current);
                if (!result.Predecessors.TryGetValue(current, out Vector2Int prev))
                    return null;
                current = prev;
            }

            path.Add(origin);
            path.Reverse();
            return path;
        }

        // Every tile attackable from any stopping point, by Manhattan distance.
        // Stopping points themselves are excluded — you can't attack where you stand.
        public static HashSet<Vector2Int> ComputeAttackRange(
            HashSet<Vector2Int> destinations,
            int minRange,
            int maxRange,
            int mapWidth,
            int mapHeight)
        {
            var attackable = new HashSet<Vector2Int>();

            foreach (Vector2Int dest in destinations)
                AddTilesInRange(dest, minRange, maxRange, mapWidth, mapHeight, attackable);

            attackable.ExceptWith(destinations);
            return attackable;
        }

        // --- Search state ---

        // Mutable scratch threaded through the flood-fill. Bundles the four
        // output collections plus the frontier so helpers don't have to pass
        // eight parameters around.
        private class SearchState
        {
            public readonly HashSet<Vector2Int> Destinations = new();
            public readonly HashSet<Vector2Int> PassThrough = new();
            public readonly Dictionary<Vector2Int, int> CostMap = new();
            public readonly Dictionary<Vector2Int, Vector2Int> Predecessors = new();
            // List-based frontier — for 32×32 grids (1024 nodes) this beats a heap.
            public readonly List<(int cost, Vector2Int pos)> Frontier = new();
        }

        private static SearchState NewSearch(Vector2Int origin)
        {
            var s = new SearchState();
            s.CostMap[origin] = 0;
            s.Destinations.Add(origin);
            s.Frontier.Add((0, origin));
            return s;
        }

        private static (int cost, Vector2Int pos) PopMinCost(List<(int cost, Vector2Int pos)> frontier)
        {
            int minIndex = FindMinIndex(frontier);
            var entry = frontier[minIndex];
            frontier[minIndex] = frontier[frontier.Count - 1];
            frontier.RemoveAt(frontier.Count - 1);
            return entry;
        }

        private static bool IsStaleFrontierEntry(int dequeuedCost, Vector2Int pos, Dictionary<Vector2Int, int> costMap)
        {
            return dequeuedCost > GetCost(costMap, pos);
        }

        // --- Neighbor expansion ---

        private static void ExpandNeighbors(
            Vector2Int current,
            int currentCost,
            int movementPoints,
            MovementType moveType,
            int mapWidth,
            int mapHeight,
            Func<int, int, TerrainType> getTerrainType,
            Func<TerrainType, TerrainStats> getTerrainStats,
            Func<Vector2Int, OccupantType> getOccupant,
            SearchState search)
        {
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                Vector2Int neighbor = current + CardinalOffsets[i];
                if (!IsInBounds(neighbor, mapWidth, mapHeight)) continue;

                int stepCost = GetStepCost(neighbor, moveType, getTerrainType, getTerrainStats);
                if (stepCost == 0) continue;

                OccupantType occupant = getOccupant(neighbor);
                if (occupant == OccupantType.Enemy) continue;

                int newCost = currentCost + stepCost;
                if (newCost > movementPoints) continue;

                RelaxEdge(neighbor, current, newCost, occupant, search);
            }
        }

        private static void RelaxEdge(
            Vector2Int neighbor,
            Vector2Int from,
            int newCost,
            OccupantType occupant,
            SearchState search)
        {
            if (!search.CostMap.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
            {
                search.CostMap[neighbor] = newCost;
                search.Predecessors[neighbor] = from;
                search.Frontier.Add((newCost, neighbor));

                ClassifyTile(neighbor, occupant, search.Destinations, search.PassThrough);
                return;
            }

            if (newCost == existingCost)
                UpdatePredecessorIfStraighter(neighbor, from, search.Predecessors);
        }

        private static int GetStepCost(
            Vector2Int pos,
            MovementType moveType,
            Func<int, int, TerrainType> getTerrainType,
            Func<TerrainType, TerrainStats> getTerrainStats)
        {
            TerrainType terrain = getTerrainType(pos.x, pos.y);
            TerrainStats stats = getTerrainStats(terrain);
            return GetMovementCost(stats, moveType);
        }

        private static void ClassifyTile(
            Vector2Int tile,
            OccupantType occupant,
            HashSet<Vector2Int> destinations,
            HashSet<Vector2Int> passThrough)
        {
            if (occupant == OccupantType.Ally)
            {
                destinations.Remove(tile);
                passThrough.Add(tile);
                return;
            }

            passThrough.Remove(tile);
            destinations.Add(tile);
        }

        // When two paths tie on cost, prefer the one that keeps going in the
        // same direction. Makes the displayed path look straight instead of
        // zig-zagging through equivalent diagonals.
        private static void UpdatePredecessorIfStraighter(
            Vector2Int node,
            Vector2Int newPredecessor,
            Dictionary<Vector2Int, Vector2Int> predecessors)
        {
            if (!predecessors.TryGetValue(node, out Vector2Int oldPredecessor))
                return;

            int oldTurns = CountDirectionChange(node, oldPredecessor, predecessors);
            int newTurns = CountDirectionChange(node, newPredecessor, predecessors);

            if (newTurns < oldTurns)
                predecessors[node] = newPredecessor;
        }

        private static int CountDirectionChange(
            Vector2Int node,
            Vector2Int predecessor,
            Dictionary<Vector2Int, Vector2Int> predecessors)
        {
            Vector2Int incomingDir = node - predecessor;
            if (!predecessors.TryGetValue(predecessor, out Vector2Int grandparent))
                return 0;

            Vector2Int previousDir = predecessor - grandparent;
            return incomingDir == previousDir ? 0 : 1;
        }

        // --- Range helpers ---

        // All tiles at Manhattan distance [min, max] from center, clipped to map.
        internal static void AddTilesInRange(
            Vector2Int center,
            int minRange,
            int maxRange,
            int mapWidth,
            int mapHeight,
            HashSet<Vector2Int> result)
        {
            for (int dist = minRange; dist <= maxRange; dist++)
            {
                for (int dx = -dist; dx <= dist; dx++)
                {
                    int dy = dist - Mathf.Abs(dx);

                    AddIfInBounds(center.x + dx, center.y + dy, mapWidth, mapHeight, result);
                    if (dy != 0)
                        AddIfInBounds(center.x + dx, center.y - dy, mapWidth, mapHeight, result);
                }
            }
        }

        private static void AddIfInBounds(int x, int y, int w, int h, HashSet<Vector2Int> set)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
                set.Add(new Vector2Int(x, y));
        }

        private static bool IsInBounds(Vector2Int pos, int w, int h)
        {
            return pos.x >= 0 && pos.x < w && pos.y >= 0 && pos.y < h;
        }

        private static int FindMinIndex(List<(int cost, Vector2Int pos)> frontier)
        {
            int minIdx = 0;
            for (int i = 1; i < frontier.Count; i++)
            {
                if (frontier[i].cost < frontier[minIdx].cost)
                    minIdx = i;
            }
            return minIdx;
        }

        private static int GetCost(Dictionary<Vector2Int, int> costMap, Vector2Int pos)
        {
            return costMap.TryGetValue(pos, out int cost) ? cost : int.MaxValue;
        }
    }
}
