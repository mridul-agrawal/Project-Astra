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

            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i].layer != layer) continue;
                int index = y * _width + x;
                int[] ids = _layers[i].tileIds;
                if (index < 0 || index >= ids.Length) return -1;
                return ids[index];
            }

            return -1;
        }

        public void SetTileId(MapLayer layer, int x, int y, int tileId)
        {
            if (!IsInBounds(x, y)) return;

            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i].layer != layer) continue;
                int index = y * _width + x;
                int[] ids = _layers[i].tileIds;
                if (index >= 0 && index < ids.Length)
                    ids[index] = tileId;
                return;
            }
        }

        public MapLayerData? GetLayerData(MapLayer layer)
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
