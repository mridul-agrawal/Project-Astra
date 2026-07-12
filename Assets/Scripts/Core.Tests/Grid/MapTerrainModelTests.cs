using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Tests.Grid
{
    [TestFixture]
    public class MapTerrainModelTests
    {
        private MapData _map;

        [SetUp]
        public void SetUp()
        {
            _map = ScriptableObject.CreateInstance<MapData>();

            var so = new UnityEditor.SerializedObject(_map);
            so.FindProperty("_width").intValue = 3;
            so.FindProperty("_height").intValue = 2;
            so.FindProperty("mapId").stringValue = "test_map";

            var terrainProp = so.FindProperty("terrain");
            terrainProp.arraySize = 6;
            // Row-major (y * width + x):
            // Row 0: Plain, Forest, Road
            // Row 1: Water, Wall,   Fort
            TerrainType[] cells =
            {
                TerrainType.Plain, TerrainType.Forest, TerrainType.Road,
                TerrainType.Water, TerrainType.Wall,   TerrainType.Fort
            };
            for (int i = 0; i < cells.Length; i++)
                terrainProp.GetArrayElementAtIndex(i).enumValueIndex = (int)cells[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_map);

        [Test]
        public void TerrainAt_ReturnsPaintedTerrain()
        {
            Assert.AreEqual(TerrainType.Plain, _map.TerrainAt(0, 0));
            Assert.AreEqual(TerrainType.Road, _map.TerrainAt(2, 0));
            Assert.AreEqual(TerrainType.Water, _map.TerrainAt(0, 1));
            Assert.AreEqual(TerrainType.Fort, _map.TerrainAt(2, 1));
        }

        [Test]
        public void TerrainAt_UsesRowMajorIndexing()
        {
            // (x=1, y=1) => index 1*3+1 = 4 => Wall
            Assert.AreEqual(TerrainType.Wall, _map.TerrainAt(1, 1));
        }

        [Test]
        public void TerrainAt_OutOfBounds_ReturnsVoid()
        {
            Assert.AreEqual(TerrainType.Void, _map.TerrainAt(-1, 0));
            Assert.AreEqual(TerrainType.Void, _map.TerrainAt(3, 0));
            Assert.AreEqual(TerrainType.Void, _map.TerrainAt(0, 2));
        }

        [Test]
        public void MapStringId_RoundTrips()
        {
            Assert.AreEqual("test_map", _map.MapStringId);
        }

        [Test]
        public void MapCatalog_GetByStringId_ResolvesMap()
        {
            var catalog = ScriptableObject.CreateInstance<MapCatalog>();

            var cso = new UnityEditor.SerializedObject(catalog);
            var mapsProp = cso.FindProperty("_maps");
            mapsProp.arraySize = 1;
            mapsProp.GetArrayElementAtIndex(0).objectReferenceValue = _map;
            cso.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreSame(_map, catalog.Get("test_map"));
            Assert.IsNull(catalog.Get("nonexistent"));

            Object.DestroyImmediate(catalog);
        }
    }
}
