using System;
using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // A single battle map's authored data: its seamless base art, a per-cell terrain grid, unit
    // start positions, and interactive objects. Pure data — no rendering. MapRenderer draws it and
    // answers terrain queries through TerrainAt; pathfinding and combat read terrain through there.
    [CreateAssetMenu(menuName = "Project Astra/Map/Map Data")]
    public class MapData : ScriptableObject
    {
        private const int MinDimension = 1;
        private const int MaxDimension = 64;

        [SerializeField] private string mapName;
        [SerializeField] private string mapId;
        [SerializeField, TextArea] private string winConditionText;
        [SerializeField, TextArea] private string loseConditionText;
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 4;
        [SerializeField] private Sprite baseArt;
        [SerializeField] private TerrainType[] terrain = Array.Empty<TerrainType>();
        [SerializeField] private UnitStartPosition[] unitStartPositions = Array.Empty<UnitStartPosition>();
        [SerializeField] private MapObject[] objects = Array.Empty<MapObject>();

        [Tooltip("Optional side objectives shown in the objectives banner. Leave empty and the banner shows the win and lose lines alone.")]
        [SerializeField] private SecondaryObjective[] secondaryObjectives = Array.Empty<SecondaryObjective>();

        [Tooltip("Per-map animated environment, authored as a prefab in the Environment Builder. Its children (trees, grass, birds, a flowing river) carry their own SpriteRenderer + Animator + behaviour components. Instantiated on load.")]
        [SerializeField] private GameObject environmentPrefab;

        public string MapName => mapName;
        public string MapId => mapId;
        public string WinConditionText => winConditionText;
        public string LoseConditionText => loseConditionText;
        public int Width => width;
        public int Height => height;
        public Sprite BaseArt => baseArt;
        public TerrainType[] Terrain => terrain;
        public UnitStartPosition[] UnitStartPositions => unitStartPositions;
        public MapObject[] Objects => objects;
        public SecondaryObjective[] SecondaryObjectives => secondaryObjectives;
        public GameObject EnvironmentPrefab => environmentPrefab;

        // Terrain for a cell, read straight from the painted grid. The single gameplay seam.
        public TerrainType TerrainAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return TerrainType.Void;
            int index = y * width + x;
            if (terrain == null || index < 0 || index >= terrain.Length) return TerrainType.Void;
            return terrain[index];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private void OnValidate()
        {
            width = Mathf.Clamp(width, MinDimension, MaxDimension);
            height = Mathf.Clamp(height, MinDimension, MaxDimension);
        }
    }

    [Serializable]
    public struct UnitStartPosition
    {
        public Vector2Int position;
        public string unitId;
        public int team;

        [Tooltip("Optional: overrides the unit definition's default loadout for this placement on this map.")]
        public InventoryLoadout loadoutOverride;
    }

    // One side objective for the objectives banner: what it asks for, whether it is done, and an
    // optional progress pair. The authored values are the map's STARTING state — a running battle
    // works on a copy, so ticking one off mid-map never writes back into the asset.
    [Serializable]
    public struct SecondaryObjective
    {
        [Tooltip("One terse line, e.g. \"Open every chest\".")]
        public string text;

        [Tooltip("Starts already complete. Normally left off.")]
        public bool complete;

        [Tooltip("Progress shown as current/max. Leave max at 0 for an objective with no counter.")]
        public int current;
        public int max;

        public bool HasCounter => max > 0;
    }

    // A placed sprite above the base art for things that change appearance mid-game
    // (tree becoming a bridge, chest opening, wall breaking). Behaviors come later; for now
    // it carries position, art, an id handle, and an optional terrain override for that cell.
    [Serializable]
    public struct MapObject
    {
        public Vector2Int position;
        public Sprite sprite;
        public string objectId;
        public bool overridesTerrain;
        public TerrainType terrainOverride;
    }
}
