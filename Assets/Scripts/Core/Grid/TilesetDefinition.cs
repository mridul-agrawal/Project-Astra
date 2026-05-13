using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]
[assembly: InternalsVisibleTo("ProjectAstra.Core.Editor")]

namespace ProjectAstra.Core.Grid
{
    // Maps integer tile IDs (the values MapData stores per cell) to renderable TileBase assets
    // plus their gameplay TerrainType. Each tileset corresponds to one sprite sheet; maps
    // reference tilesets by index. All lookups are O(1) array access.
    [CreateAssetMenu(menuName = "Project Astra/Map/Tileset Definition")]
    public class TilesetDefinition : ScriptableObject
    {
        [SerializeField] private string _tilesetName;
        [SerializeField] private Texture2D _sourceTexture;
        [SerializeField] private int _tilePixelSize = 16;

        // Indexed by tile ID (row-major position in the sprite sheet, starting from 0).
        [SerializeField] private TileEntry[] _tiles = Array.Empty<TileEntry>();

        public string TilesetName => _tilesetName;
        public Texture2D SourceTexture => _sourceTexture;
        public int TilePixelSize => _tilePixelSize;
        public int TileCount => _tiles.Length;

        public TileBase GetTile(int id)
        {
            if (!IsValidId(id)) return null;
            return _tiles[id].tileAsset;
        }

        public TerrainType GetTerrainType(int id)
        {
            if (!IsValidId(id)) return TerrainType.Void;
            return _tiles[id].terrainType;
        }

        public bool IsValidId(int id) => id >= 0 && id < _tiles.Length;

        // Setters used only by editor tooling (PlaceholderTilesetGenerator, TilesetDefinitionEditor).
        internal void SetTiles(TileEntry[] tiles) => _tiles = tiles;
        internal void SetTilesetName(string name) => _tilesetName = name;
    }

    // Pairs a renderable tile with its terrain classification. Putting terrain on the tile
    // (instead of on the map cell) means swapping tilesets changes visuals AND gameplay data
    // in lockstep — no risk of a forest tile rendering as a wall, or vice versa.
    [Serializable]
    public struct TileEntry
    {
        public TileBase tileAsset;
        public TerrainType terrainType;
    }
}
