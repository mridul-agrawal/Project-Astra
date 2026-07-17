using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Single source of truth for "what terrain is on a tile, and what that terrain does".
    // Populated once per map load by MapBootstrapper, then read anywhere via
    // MapService.Instance — no scene lookup, no per-consumer serialized reference.
    // Same shape as TurnManager's UnitRegistry: a plain service reached through a
    // static handle set at one known seam.
    public class MapService
    {
        public static MapService Instance { get; private set; }

        private readonly MapData map;
        private readonly TerrainStatTable terrainStats;

        private MapService(MapData map, TerrainStatTable terrainStats)
        {
            this.map = map;
            this.terrainStats = terrainStats;
        }

        // The single set-point. MapBootstrapper calls this when a map loads.
        public static void Load(MapData map, TerrainStatTable terrainStats)
        {
            Instance = new MapService(map, terrainStats);
        }

        public MapData CurrentMap => map;

        // Void when there is no map or the cell is out of bounds — never throws.
        public TerrainType GetTerrainType(Vector2Int position)
        {
            return map != null ? map.TerrainAt(position.x, position.y) : TerrainType.Void;
        }

        public TerrainStats GetStats(Vector2Int position)
        {
            return GetStats(GetTerrainType(position));
        }

        public TerrainStats GetStats(TerrainType terrain)
        {
            return terrainStats != null ? terrainStats.GetStats(terrain) : TerrainStats.Default;
        }
    }
}
