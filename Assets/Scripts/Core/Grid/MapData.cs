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

        [Tooltip("Optional prefab of animated environment — decorations (trees, grass, birds) and base-layer patches (a flowing river). Instantiated on load; each child carries its own SpriteRenderer + Animator.")]
        [SerializeField] private GameObject environmentPrefab;

        [Tooltip("Authored animated decorations (deco + base-layer river patches), placed by the Map Editor's Environment tab and spawned at load.")]
        [SerializeField] private EnvironmentDecoration[] decorations = Array.Empty<EnvironmentDecoration>();

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
        public GameObject EnvironmentPrefab => environmentPrefab;
        public EnvironmentDecoration[] Decorations => decorations;

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

    // An authored animated decoration: a looping animator at a free pixel position,
    // with sorting, phase, optional tiling (base-layer river patches), and optional
    // point-to-point motion (birds). Spawned by MapRenderer at load; previewSprite
    // is its first frame, cached by the editor so the runtime needs no AnimationUtility.
    [Serializable]
    public struct EnvironmentDecoration
    {
        public string id;
        public Vector2 position;
        public RuntimeAnimatorController animator;
        public Sprite previewSprite;
        public string sortingLayer;
        public int sortingOrder;
        [Range(0f, 1f)] public float phaseOffset;
        public bool useUnscaledTime;
        public Vector2 tileSize;
        public bool moves;
        public Vector2 waypointOffset;
        public float moveSpeed;
        public bool flipToFaceTravel;
    }
}
