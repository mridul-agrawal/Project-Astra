using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Pathfinding
{
    // Wires the stateless Pathfinder to live game systems (MapRenderer +
    // TerrainStatTable). Occupancy is passed in as a delegate because callers
    // own the unit-position lookup, not this service.
    public class PathfindingService
    {
        private readonly MapRenderer mapRenderer;
        private readonly TerrainStatTable terrainStatTable;

        // Fires when a runtime cell change invalidates any cached reachability.
        public event Action OnMapChanged;

        public PathfindingService(MapRenderer mapRenderer, TerrainStatTable terrainStatTable)
        {
            this.mapRenderer = mapRenderer;
            this.terrainStatTable = terrainStatTable;
            mapRenderer.OnCellChanged += HandleCellChanged;
        }

        public Pathfinder.ReachabilityResult ComputeReachability(
            Vector2Int origin,
            int movementPoints,
            MovementType moveType,
            Func<Vector2Int, Pathfinder.OccupantType> getOccupant)
        {
            var map = mapRenderer.CurrentMap;
            return Pathfinder.ComputeReachability(
                origin, movementPoints, moveType,
                map.Width, map.Height,
                (x, y) => mapRenderer.GetTerrainType(x, y),
                t => terrainStatTable.GetStats(t),
                getOccupant);
        }

        public List<Vector2Int> ReconstructPath(
            Vector2Int origin,
            Vector2Int destination,
            Pathfinder.ReachabilityResult result)
        {
            return Pathfinder.ReconstructPath(origin, destination, result);
        }

        public HashSet<Vector2Int> ComputeAttackRange(
            HashSet<Vector2Int> destinations,
            int minRange,
            int maxRange)
        {
            var map = mapRenderer.CurrentMap;
            return Pathfinder.ComputeAttackRange(destinations, minRange, maxRange,
                map.Width, map.Height);
        }

        private void HandleCellChanged(Vector2Int position)
        {
            OnMapChanged?.Invoke();
        }
    }
}
