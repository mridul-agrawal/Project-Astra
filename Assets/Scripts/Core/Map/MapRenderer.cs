using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Orchestrates tilemap rendering for a loaded map. Reads MapData (integer tile IDs),
    /// resolves them through TilesetDefinitions into Unity TileBase assets, and stamps them
    /// onto the appropriate Tilemap layer. Also provides runtime tile swapping (for destructible
    /// walls) and terrain type queries (for movement/combat systems).
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        // One Tilemap per render layer: Ground, Overlay, Object, Units, UIOverlay
        [Header("Tilemap Layers")]
        [SerializeField] private Tilemap[] _tilemaps = new Tilemap[5];

        // Magenta tile shown when a tile ID is invalid or missing
        [Header("Fallback")]
        [SerializeField] private TileBase _errorTile;

        private MapData _currentMap;

        public MapData CurrentMap => _currentMap;

        /// <summary>Fired when SwapTile changes a tile, carrying the cell position. Pathfinding subscribes to this.</summary>
        public event Action<Vector2Int> OnTileSwapped;

        /// <summary>Clears all tilemaps, then stamps every layer from the given MapData.</summary>
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
            ValidateGroundLayer(mapData);
        }

        /// <summary>
        /// Replaces a single tile at runtime (e.g. destructible wall → rubble).
        /// Updates both the visual tilemap and the backing MapData in a single frame.
        /// </summary>
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

        /// <summary>Returns the terrain type at a ground-layer cell. Used by movement, combat, and pathfinding.</summary>
        public TerrainType GetTerrainType(int x, int y)
        {
            if (_currentMap == null) return TerrainType.Void;

            int groundTileId = _currentMap.GetTileId(MapLayer.Ground, x, y);
            if (groundTileId < 0) return TerrainType.Void;

            TilesetDefinition tileset = GetGroundTileset();
            if (tileset == null) return TerrainType.Void;

            return tileset.GetTerrainType(groundTileId);
        }

        // --- Map loading helpers ---

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

        /// <summary>Iterates every cell in a layer and places the resolved tile onto the tilemap.</summary>
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

        // --- Tile resolution ---

        /// <summary>Converts a tile ID to a TileBase via the tileset, falling back to the error tile on failure.</summary>
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

        // --- Validation ---

        /// <summary>Warns about cells with no ground tile — a map authoring error that renders as a black void.</summary>
        private void ValidateGroundLayer(MapData mapData)
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

        // --- Lookup helpers ---

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
