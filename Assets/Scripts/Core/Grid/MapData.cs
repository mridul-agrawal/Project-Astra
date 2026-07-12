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

        [SerializeField] private string _mapName;
        [SerializeField] private string mapId;
        [SerializeField] private int _width = 4;
        [SerializeField] private int _height = 4;
        [SerializeField] private Sprite baseArt;
        [SerializeField] private TerrainType[] terrain = Array.Empty<TerrainType>();
        [SerializeField] private UnitStartPosition[] _unitStartPositions = Array.Empty<UnitStartPosition>();
        [SerializeField] private MapObject[] objects = Array.Empty<MapObject>();

        public string MapName => _mapName;
        public string MapId => mapId;
        public int Width => _width;
        public int Height => _height;
        public Sprite BaseArt => baseArt;
        public TerrainType[] Terrain => terrain;
        public UnitStartPosition[] UnitStartPositions => _unitStartPositions;
        public MapObject[] Objects => objects;

        // Terrain for a cell, read straight from the painted grid. The single gameplay seam.
        public TerrainType TerrainAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return TerrainType.Void;
            int index = y * _width + x;
            if (terrain == null || index < 0 || index >= terrain.Length) return TerrainType.Void;
            return terrain[index];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private void OnValidate()
        {
            _width = Mathf.Clamp(_width, MinDimension, MaxDimension);
            _height = Mathf.Clamp(_height, MinDimension, MaxDimension);
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
}
