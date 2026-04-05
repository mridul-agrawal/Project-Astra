using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Wires the stateless Pathfinder to live game systems (MapRenderer + TerrainStatTable).
    /// Not a MonoBehaviour — instantiated by whatever system needs pathfinding.
    /// Occupancy is passed in as a delegate since the unit management system doesn't exist yet.
    /// </summary>
    public class PathfindingService
    {
        private readonly MapRenderer _mapRenderer;
        private readonly TerrainStatTable _terrainStatTable;

        /// <summary>Fired when the map changes (tile swap). Callers who cache results should re-compute.</summary>
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
