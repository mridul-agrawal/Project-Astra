using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]
[assembly: InternalsVisibleTo("ProjectAstra.Core.Editor")]

namespace ProjectAstra.Core
{
    /// <summary>
    /// Bridges integer tile IDs (stored in MapData) to Unity TileBase assets and TerrainType metadata.
    /// Each tileset corresponds to one sprite sheet. Maps reference tilesets by index, and store
    /// per-cell tile IDs that this class resolves to renderable tiles and gameplay-relevant terrain types.
    /// All lookups are O(1) array index access.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Astra/Map/Tileset Definition")]
    public class TilesetDefinition : ScriptableObject
    {
        [SerializeField] private string _tilesetName;
        [SerializeField] private Texture2D _sourceTexture;
        [SerializeField] private int _tilePixelSize = 16;

        // Indexed by tile ID (row-major position in the sprite sheet, starting from 0)
        [SerializeField] private TileEntry[] _tiles = Array.Empty<TileEntry>();

        public string TilesetName => _tilesetName;
        public Texture2D SourceTexture => _sourceTexture;
        public int TilePixelSize => _tilePixelSize;
        public int TileCount => _tiles.Length;

        /// <summary>Returns the Unity TileBase for rendering, or null if the ID is invalid.</summary>
        public TileBase GetTile(int id)
        {
            if (!IsValidId(id)) return null;
            return _tiles[id].tileAsset;
        }

        /// <summary>Returns the terrain type bound to this tile ID, used by movement/combat systems.</summary>
        public TerrainType GetTerrainType(int id)
        {
            if (!IsValidId(id)) return TerrainType.Void;
            return _tiles[id].terrainType;
        }

        public bool IsValidId(int id)
        {
            return id >= 0 && id < _tiles.Length;
        }

        // Internal setters used by editor tooling (PlaceholderTilesetGenerator, TilesetDefinitionEditor)
        internal void SetTiles(TileEntry[] tiles) => _tiles = tiles;
        internal void SetTilesetName(string name) => _tilesetName = name;
    }

    /// <summary>
    /// Pairs a renderable tile asset with its gameplay terrain classification.
    /// Terrain type is a property of the tile itself, not the map cell — swapping
    /// tilesets changes both visuals and gameplay data consistently.
    /// </summary>
    [Serializable]
    public struct TileEntry
    {
        public TileBase tileAsset;
        public TerrainType terrainType;
    }
}
