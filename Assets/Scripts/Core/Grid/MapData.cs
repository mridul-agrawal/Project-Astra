using System;
using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // A single battle map's authored data: dimensions, per-layer tile IDs, unit start
    // positions, and event triggers. Pure data — no rendering. MapRenderer is what stamps
    // this onto Unity tilemaps; pathfinding and combat read terrain through here too.
    [CreateAssetMenu(menuName = "Project Astra/Map/Map Data")]
    public class MapData : ScriptableObject
    {
        private const int MinDimension = 1;
        private const int MaxDimension = 64;

        [SerializeField] private string _mapName;
        [SerializeField] private int _width = 4;
        [SerializeField] private int _height = 4;
        [SerializeField] private TilesetDefinition[] _tilesets = Array.Empty<TilesetDefinition>();
        [SerializeField] private MapLayerData[] _layers = Array.Empty<MapLayerData>();
        [SerializeField] private UnitStartPosition[] _unitStartPositions = Array.Empty<UnitStartPosition>();
        [SerializeField] private EventTrigger[] _eventTriggers = Array.Empty<EventTrigger>();

        public string MapName => _mapName;
        public int Width => _width;
        public int Height => _height;
        public TilesetDefinition[] Tilesets => _tilesets;
        public MapLayerData[] Layers => _layers;
        public UnitStartPosition[] UnitStartPositions => _unitStartPositions;
        public EventTrigger[] EventTriggers => _eventTriggers;

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public int GetTileId(MapLayer layer, int x, int y)
        {
            if (!TryResolveCell(layer, x, y, out int[] layerTiles, out int index))
                return -1;

            return layerTiles[index];
        }

        public void SetTileId(MapLayer layer, int x, int y, int tileId)
        {
            if (!TryResolveCell(layer, x, y, out int[] layerTiles, out int index))
                return;

            layerTiles[index] = tileId;
        }

        public MapLayerData? GetLayerData(MapLayer layer)
        {
            return FindLayer(layer);
        }

        // Locates the cell's backing tile array and its flat index; false if it can't.
        private bool TryResolveCell(MapLayer layer, int x, int y, out int[] layerTiles, out int index)
        {
            layerTiles = null;
            index = -1;

            if (!IsInBounds(x, y)) return false;
            if (!TryGetLayerTiles(layer, out int[] tiles)) return false;

            int flatIndex = ToFlatIndex(x, y);
            if (!IsWithinLayer(tiles, flatIndex)) return false;

            layerTiles = tiles;
            index = flatIndex;
            return true;
        }

        private bool TryGetLayerTiles(MapLayer layer, out int[] tiles)
        {
            MapLayerData? layerData = FindLayer(layer);
            if (!layerData.HasValue)
            {
                tiles = null;
                return false;
            }

            tiles = layerData.Value.tileIds;
            return true;
        }

        private bool IsWithinLayer(int[] tiles, int flatIndex) => flatIndex < tiles.Length;

        private int ToFlatIndex(int x, int y) => y * _width + x;

        private MapLayerData? FindLayer(MapLayer layer)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i].layer == layer)
                    return _layers[i];
            }
            return null;
        }

        private void OnValidate()
        {
            _width = Mathf.Clamp(_width, MinDimension, MaxDimension);
            _height = Mathf.Clamp(_height, MinDimension, MaxDimension);
        }
    }

    [Serializable]
    public struct MapLayerData
    {
        public MapLayer layer;
        public int tilesetIndex;
        public int[] tileIds;
    }

    [Serializable]
    public struct UnitStartPosition
    {
        public Vector2Int position;
        public string unitId;
        public int team;
    }

    [Serializable]
    public struct EventTrigger
    {
        public Vector2Int position;
        public string eventId;
    }
}
