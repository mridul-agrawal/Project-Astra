using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core
{
    public class MapRenderer : MonoBehaviour
    {
        [Header("Tilemap Layers")]
        [SerializeField] private Tilemap[] _tilemaps = new Tilemap[5];

        [Header("Fallback")]
        [SerializeField] private TileBase _errorTile;

        private MapData _currentMap;

        public MapData CurrentMap => _currentMap;

        public event Action<Vector2Int> OnTileSwapped;

        public void LoadMap(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("MapRenderer: Cannot load null MapData.");
                return;
            }

            _currentMap = mapData;
            ClearAllTilemaps();

            foreach (var layerData in mapData.Layers)
            {
                int layerIndex = (int)layerData.layer;
                if (layerIndex < 0 || layerIndex >= _tilemaps.Length || _tilemaps[layerIndex] == null)
                {
                    Debug.LogWarning($"MapRenderer: No tilemap assigned for layer {layerData.layer}.");
                    continue;
                }

                var tileset = GetTileset(mapData, layerData.tilesetIndex);
                if (tileset == null) continue;

                var tilemap = _tilemaps[layerIndex];
                StampLayer(tilemap, mapData, layerData, tileset);
            }

            ValidateGroundLayer(mapData);
        }

        public void SwapTile(MapLayer layer, int x, int y, int newTileId, int tilesetIndex)
        {
            if (_currentMap == null) return;
            if (!_currentMap.IsInBounds(x, y)) return;

            int layerIndex = (int)layer;
            if (layerIndex < 0 || layerIndex >= _tilemaps.Length || _tilemaps[layerIndex] == null) return;

            var tileset = GetTileset(_currentMap, tilesetIndex);
            if (tileset == null) return;

            _currentMap.SetTileId(layer, x, y, newTileId);

            TileBase tile = ResolveTile(tileset, newTileId, x, y);
            _tilemaps[layerIndex].SetTile(new Vector3Int(x, y, 0), tile);

            OnTileSwapped?.Invoke(new Vector2Int(x, y));
        }

        public TerrainType GetTerrainType(int x, int y)
        {
            if (_currentMap == null) return TerrainType.Void;

            int groundTileId = _currentMap.GetTileId(MapLayer.Ground, x, y);
            if (groundTileId < 0) return TerrainType.Void;

            var layerData = _currentMap.GetLayerData(MapLayer.Ground);
            if (!layerData.HasValue) return TerrainType.Void;

            var tileset = GetTileset(_currentMap, layerData.Value.tilesetIndex);
            if (tileset == null) return TerrainType.Void;

            return tileset.GetTerrainType(groundTileId);
        }

        private void StampLayer(Tilemap tilemap, MapData mapData, MapLayerData layerData,
            TilesetDefinition tileset)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int index = y * mapData.Width + x;
                    if (index >= layerData.tileIds.Length) continue;

                    int tileId = layerData.tileIds[index];
                    if (tileId < 0) continue;

                    TileBase tile = ResolveTile(tileset, tileId, x, y);
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private TileBase ResolveTile(TilesetDefinition tileset, int tileId, int x, int y)
        {
            if (!tileset.IsValidId(tileId))
            {
                Debug.LogError($"MapRenderer: Invalid tile ID {tileId} at ({x},{y}). Using error tile.");
                return _errorTile;
            }

            TileBase tile = tileset.GetTile(tileId);
            if (tile == null)
            {
                Debug.LogError($"MapRenderer: Null TileBase for ID {tileId} at ({x},{y}). Using error tile.");
                return _errorTile;
            }

            return tile;
        }

        private void ValidateGroundLayer(MapData mapData)
        {
            var groundLayer = mapData.GetLayerData(MapLayer.Ground);
            if (!groundLayer.HasValue)
            {
                Debug.LogWarning("MapRenderer: Map has no Ground layer defined.");
                return;
            }

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int tileId = mapData.GetTileId(MapLayer.Ground, x, y);
                    if (tileId < 0)
                    {
                        Debug.LogWarning($"MapRenderer: Missing ground tile at ({x},{y}). " +
                                         "This is a map authoring error.");
                    }
                }
            }
        }

        private TilesetDefinition GetTileset(MapData mapData, int tilesetIndex)
        {
            if (tilesetIndex < 0 || tilesetIndex >= mapData.Tilesets.Length)
            {
                Debug.LogError($"MapRenderer: Invalid tileset index {tilesetIndex}.");
                return null;
            }
            return mapData.Tilesets[tilesetIndex];
        }

        private void ClearAllTilemaps()
        {
            foreach (var tilemap in _tilemaps)
            {
                if (tilemap != null)
                    tilemap.ClearAllTiles();
            }
        }
    }
}
