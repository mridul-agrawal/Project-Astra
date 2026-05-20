using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core.Grid
{
    // Stamps a MapData onto Unity tilemaps and supports runtime tile swaps for destructibles
    // (walls becoming rubble, etc.). Also exposes terrain-type queries for movement and combat.
    public class MapRenderer : MonoBehaviour
    {
        // One Tilemap per MapLayer (Ground / Overlay / Object / Units / UI), wired in the Inspector.
        [Header("Tilemap Layers")]
        [SerializeField] private Tilemap[] _tilemaps = new Tilemap[5];

        // Shown when a tile ID is invalid or its TileBase asset is missing — usually a bright
        // magenta so authoring errors are loud.
        [Header("Fallback")]
        [SerializeField] private TileBase _errorTile;

        private MapData _currentMap;

        public MapData CurrentMap => _currentMap;

        // Fires when SwapTile mutates a cell. Pathfinding listens so it can rebuild its graph.
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
            StampAllLayers(mapData);
            WarnIfGroundLayerHasHoles(mapData);
        }

        // Runtime replacement for a single tile (e.g. destructible wall → rubble). Updates the
        // tilemap visual AND the backing MapData in one call so both stay consistent.
        public void SwapTile(MapLayer layer, int x, int y, int newTileId, int tilesetIndex)
        {
            if (_currentMap == null) return;
            if (!_currentMap.IsInBounds(x, y)) return;

            Tilemap tilemap = GetTilemapForLayer(layer);
            if (tilemap == null) return;

            TilesetDefinition tileset = GetTileset(_currentMap, tilesetIndex);
            if (tileset == null) return;

            _currentMap.SetTileId(layer, x, y, newTileId);

            TileBase tile = ResolveTile(tileset, newTileId, x, y);
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);

            OnTileSwapped?.Invoke(new Vector2Int(x, y));
        }

        public TerrainType GetTerrainType(int x, int y)
        {
            if (_currentMap == null) return TerrainType.Void;

            int groundTileId = _currentMap.GetTileId(MapLayer.Ground, x, y);
            if (groundTileId < 0) return TerrainType.Void;

            TilesetDefinition tileset = GetGroundTileset();
            if (tileset == null) return TerrainType.Void;

            return tileset.GetTerrainType(groundTileId);
        }

        private void StampAllLayers(MapData mapData)
        {
            foreach (var layerData in mapData.Layers)
            {
                Tilemap tilemap = GetTilemapForLayer(layerData.layer);
                if (tilemap == null)
                {
                    Debug.LogWarning($"MapRenderer: No tilemap assigned for layer {layerData.layer}.");
                    continue;
                }

                TilesetDefinition tileset = GetTileset(mapData, layerData.tilesetIndex);
                if (tileset == null) continue;

                StampLayer(tilemap, mapData, layerData, tileset);
            }
        }

        private void StampLayer(Tilemap tilemap, MapData mapData, MapLayerData layerData,
            TilesetDefinition tileset)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int tileId = mapData.GetTileId(layerData.layer, x, y);
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

        // A missing ground tile renders as a black void in-game — almost always a map-authoring
        // mistake. Warn once per missing cell so it's caught early.
        private void WarnIfGroundLayerHasHoles(MapData mapData)
        {
            if (!mapData.GetLayerData(MapLayer.Ground).HasValue)
            {
                Debug.LogWarning("MapRenderer: Map has no Ground layer defined.");
                return;
            }

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (mapData.GetTileId(MapLayer.Ground, x, y) < 0)
                        Debug.LogWarning($"MapRenderer: Missing ground tile at ({x},{y}). Map authoring error.");
                }
            }
        }

        private Tilemap GetTilemapForLayer(MapLayer layer)
        {
            int index = (int)layer;
            if (index < 0 || index >= _tilemaps.Length) return null;
            return _tilemaps[index];
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

        private TilesetDefinition GetGroundTileset()
        {
            MapLayerData? groundLayer = _currentMap.GetLayerData(MapLayer.Ground);
            if (!groundLayer.HasValue) return null;
            return GetTileset(_currentMap, groundLayer.Value.tilesetIndex);
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
