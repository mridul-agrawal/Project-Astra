using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]
[assembly: InternalsVisibleTo("ProjectAstra.Core.Editor")]

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Map/Tileset Definition")]
    public class TilesetDefinition : ScriptableObject
    {
        [SerializeField] private string _tilesetName;
        [SerializeField] private Texture2D _sourceTexture;
        [SerializeField] private int _tilePixelSize = 16;
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

        public bool IsValidId(int id)
        {
            return id >= 0 && id < _tiles.Length;
        }

        internal void SetTiles(TileEntry[] tiles)
        {
            _tiles = tiles;
        }

        internal void SetTilesetName(string name)
        {
            _tilesetName = name;
        }
    }

    [Serializable]
    public struct TileEntry
    {
        public TileBase tileAsset;
        public TerrainType terrainType;
    }
}
