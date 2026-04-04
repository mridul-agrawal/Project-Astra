using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class TilesetDefinitionTests
    {
        private TilesetDefinition _tileset;

        [SetUp]
        public void SetUp()
        {
            _tileset = ScriptableObject.CreateInstance<TilesetDefinition>();

            var tile0 = ScriptableObject.CreateInstance<Tile>();
            var tile1 = ScriptableObject.CreateInstance<Tile>();

            _tileset.SetTiles(new TileEntry[]
            {
                new TileEntry { tileAsset = tile0, terrainType = TerrainType.Plain },
                new TileEntry { tileAsset = tile1, terrainType = TerrainType.Forest }
            });

            _tileset.SetTilesetName("TestTileset");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tileset);
        }

        [Test]
        public void GetTile_ValidId_ReturnsTile()
        {
            Assert.IsNotNull(_tileset.GetTile(0));
            Assert.IsNotNull(_tileset.GetTile(1));
        }

        [Test]
        public void GetTile_InvalidId_ReturnsNull()
        {
            Assert.IsNull(_tileset.GetTile(-1));
            Assert.IsNull(_tileset.GetTile(2));
            Assert.IsNull(_tileset.GetTile(999));
        }

        [Test]
        public void GetTerrainType_ValidId_ReturnsCorrectType()
        {
            Assert.AreEqual(TerrainType.Plain, _tileset.GetTerrainType(0));
            Assert.AreEqual(TerrainType.Forest, _tileset.GetTerrainType(1));
        }

        [Test]
        public void GetTerrainType_InvalidId_ReturnsVoid()
        {
            Assert.AreEqual(TerrainType.Void, _tileset.GetTerrainType(-1));
            Assert.AreEqual(TerrainType.Void, _tileset.GetTerrainType(100));
        }

        [Test]
        public void IsValidId_ReturnsCorrectly()
        {
            Assert.IsTrue(_tileset.IsValidId(0));
            Assert.IsTrue(_tileset.IsValidId(1));
            Assert.IsFalse(_tileset.IsValidId(-1));
            Assert.IsFalse(_tileset.IsValidId(2));
        }

        [Test]
        public void TileCount_ReturnsCorrectCount()
        {
            Assert.AreEqual(2, _tileset.TileCount);
        }

        [Test]
        public void TilesetName_ReturnsSetName()
        {
            Assert.AreEqual("TestTileset", _tileset.TilesetName);
        }
    }
}
