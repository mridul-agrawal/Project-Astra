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
        private readonly MapRenderer _mapRenderer;
        private readonly TerrainStatTable _terrainStatTable;

        // Fires when a tile swap invalidates any cached reachability.
        public event Action OnMapChanged;

        public PathfindingService(MapRenderer mapRenderer, TerrainStatTable terrainStatTable)
        {
            _mapRenderer = mapRenderer;
            _terrainStatTable = terrainStatTable;
            _mapRenderer.OnTileSwapped += HandleTileSwapped;
        }

        public Pathfinder.ReachabilityResult ComputeReachability(
            Vector2Int origin,
            int movementPoints,
            MovementType moveType,
            Func<Vector2Int, Pathfinder.OccupantType> getOccupant)
        {
            var map = _mapRenderer.CurrentMap;
            return Pathfinder.ComputeReachability(
                origin, movementPoints, moveType,
                map.Width, map.Height,
                (x, y) => _mapRenderer.GetTerrainType(x, y),
                t => _terrainStatTable.GetStats(t),
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
            var map = _mapRenderer.CurrentMap;
            return Pathfinder.ComputeAttackRange(destinations, minRange, maxRange,
                map.Width, map.Height);
        }

        private void HandleTileSwapped(Vector2Int position)
        {
            OnMapChanged?.Invoke();
        }
    }
}
