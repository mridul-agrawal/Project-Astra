using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Static utility class providing Dijkstra-based pathfinding for the tactical grid.
    /// All methods are stateless and take terrain/occupancy lookups as delegates,
    /// making the algorithm fully testable without Unity dependencies.
    /// </summary>
    public static class Pathfinder
    {
        public enum OccupantType { None, Ally, Enemy }

        // Horizontal first (E, W) then vertical (N, S) for deterministic tie-breaking
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        /// <summary>
        /// Holds the result of a Dijkstra flood-fill. Destinations and PassThrough are
        /// kept separate so the UI can render them differently and movement logic knows
        /// which tiles are valid stopping points.
        /// </summary>
        public readonly struct ReachabilityResult
        {
            /// <summary>Tiles the unit can legally stop on.</summary>
            public readonly HashSet<Vector2Int> Destinations;

            /// <summary>Tiles the unit can move through but not stop on (ally-occupied).</summary>
            public readonly HashSet<Vector2Int> PassThrough;

            /// <summary>Minimum movement cost to reach each explored tile.</summary>
            public readonly Dictionary<Vector2Int, int> CostMap;

            /// <summary>Predecessor of each tile on the optimal path from origin.</summary>
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

        /// <summary>Returns the movement cost for a given terrain and movement type. 0 = impassable.</summary>
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

        /// <summary>
        /// Dijkstra flood-fill from origin. Returns all reachable tiles split into
        /// Destinations (can stop) and PassThrough (can traverse, ally-occupied).
        /// </summary>
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
            var destinations = new HashSet<Vector2Int>();
            var passThrough = new HashSet<Vector2Int>();
            var costMap = new Dictionary<Vector2Int, int>();
            var predecessors = new Dictionary<Vector2Int, Vector2Int>();
            // Simple list-based frontier — for grids up to 32×32 (1024 nodes) this is fast enough.
            var frontier = new List<(int cost, Vector2Int pos)>();

            costMap[origin] = 0;
            destinations.Add(origin);
            frontier.Add((0, origin));

            while (frontier.Count > 0)
            {
                int minIndex = FindMinIndex(frontier);
                var (dequeuedCost, current) = frontier[minIndex];
                frontier[minIndex] = frontier[frontier.Count - 1];
                frontier.RemoveAt(frontier.Count - 1);

                // Skip stale entries (node was already processed at a lower cost)
                if (dequeuedCost > GetCost(costMap, current))
                    continue;

                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector2Int neighbor = current + CardinalOffsets[i];

                    if (!IsInBounds(neighbor, mapWidth, mapHeight))
                        continue;

                    int stepCost = GetStepCost(neighbor, moveType, getTerrainType, getTerrainStats);
                    if (stepCost == 0) continue; // impassable terrain

                    OccupantType occupant = getOccupant(neighbor);
                    if (occupant == OccupantType.Enemy) continue; // enemy blocks completely

                    int newCost = dequeuedCost + stepCost;
                    if (newCost > movementPoints) continue; // exceeds movement budget

                    if (!costMap.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                    {
                        costMap[neighbor] = newCost;
                        predecessors[neighbor] = current;
                        frontier.Add((newCost, neighbor));

                        ClassifyTile(neighbor, occupant, destinations, passThrough);
                    }
                    else if (newCost == existingCost)
                    {
                        UpdatePredecessorIfStraighter(neighbor, current, predecessors);
                    }
                }
            }

            return new ReachabilityResult(destinations, passThrough, costMap, predecessors);
        }

        /// <summary>
        /// Reconstructs the optimal path from origin to destination using the predecessor map.
        /// Returns null if destination is not reachable.
        /// </summary>
        public static List<Vector2Int> ReconstructPath(
            Vector2Int origin,
            Vector2Int destination,
            ReachabilityResult result)
        {
            if (!result.CostMap.ContainsKey(destination))
                return null;

            if (!result.Destinations.Contains(destination))
                return null;

            var path = new List<Vector2Int>();
            Vector2Int current = destination;

            while (current != origin)
            {
                path.Add(current);
                if (!result.Predecessors.TryGetValue(current, out Vector2Int prev))
                    return null; // broken chain
                current = prev;
            }

            path.Add(origin);
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Computes all tiles attackable from any tile in the destination set,
        /// using Manhattan distance within [minRange, maxRange].
        /// </summary>
        public static HashSet<Vector2Int> ComputeAttackRange(
            HashSet<Vector2Int> destinations,
            int minRange,
            int maxRange,
            int mapWidth,
            int mapHeight)
        {
            var attackable = new HashSet<Vector2Int>();

            foreach (Vector2Int dest in destinations)
            {
                AddTilesInRange(dest, minRange, maxRange, mapWidth, mapHeight, attackable);
            }

            // Remove tiles that are destinations themselves (can't attack where you stand)
            attackable.ExceptWith(destinations);
            return attackable;
        }

        // --- Private helpers ---

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
            // Remove from previous classification if reclassifying
            if (occupant == OccupantType.Ally)
            {
                destinations.Remove(tile);
                passThrough.Add(tile);
            }
            else
            {
                passThrough.Remove(tile);
                destinations.Add(tile);
            }
        }

        /// <summary>
        /// When two paths have equal cost, prefer the one that continues in the same
        /// direction (fewer turns). This produces aesthetically straight paths.
        /// </summary>
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
                return 0; // no grandparent — no turn possible

            Vector2Int previousDir = predecessor - grandparent;
            return incomingDir == previousDir ? 0 : 1;
        }

        /// <summary>Adds all tiles at Manhattan distance [minRange, maxRange] from center.</summary>
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
