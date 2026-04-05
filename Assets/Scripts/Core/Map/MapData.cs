using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Map/Map Data")]
    public class MapData : ScriptableObject
    {
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
            if (!IsInBounds(x, y)) return -1;

            MapLayerData? layerData = FindLayer(layer);
            if (!layerData.HasValue) return -1;

            int index = ToFlatIndex(x, y);
            if (index >= layerData.Value.tileIds.Length) return -1;

            return layerData.Value.tileIds[index];
        }

        public void SetTileId(MapLayer layer, int x, int y, int tileId)
        {
            if (!IsInBounds(x, y)) return;

            MapLayerData? layerData = FindLayer(layer);
            if (!layerData.HasValue) return;

            int index = ToFlatIndex(x, y);
            if (index < layerData.Value.tileIds.Length)
                layerData.Value.tileIds[index] = tileId;
        }

        public MapLayerData? GetLayerData(MapLayer layer)
        {
            return FindLayer(layer);
        }

        // Converts (x, y) grid coordinates to an index into the flat tile ID array (row-major order)
        private int ToFlatIndex(int x, int y)
        {
            return y * _width + x;
        }

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
            _width = Mathf.Clamp(_width, 1, 64);
            _height = Mathf.Clamp(_height, 1, 64);
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
