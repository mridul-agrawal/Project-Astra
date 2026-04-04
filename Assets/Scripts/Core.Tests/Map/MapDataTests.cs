using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class MapDataTests
    {
        private MapData _mapData;

        [SetUp]
        public void SetUp()
        {
            _mapData = ScriptableObject.CreateInstance<MapData>();

            // Use SerializedObject to set private fields
            var so = new UnityEditor.SerializedObject(_mapData);
            so.FindProperty("_width").intValue = 3;
            so.FindProperty("_height").intValue = 3;

            var layersProp = so.FindProperty("_layers");
            layersProp.arraySize = 1;

            var layerElement = layersProp.GetArrayElementAtIndex(0);
            layerElement.FindPropertyRelative("layer").enumValueIndex = (int)MapLayer.Ground;
            layerElement.FindPropertyRelative("tilesetIndex").intValue = 0;

            var tileIdsProp = layerElement.FindPropertyRelative("tileIds");
            tileIdsProp.arraySize = 9;
            // Row-major: y * width + x
            // Row 0: tiles 0, 1, 2
            // Row 1: tiles 3, 4, 5
            // Row 2: tiles 6, 7, -1 (empty)
            int[] ids = { 0, 1, 2, 3, 4, 5, 6, 7, -1 };
            for (int i = 0; i < 9; i++)
                tileIdsProp.GetArrayElementAtIndex(i).intValue = ids[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mapData);
        }

        [Test]
        public void IsInBounds_ValidCoords_ReturnsTrue()
        {
            Assert.IsTrue(_mapData.IsInBounds(0, 0));
            Assert.IsTrue(_mapData.IsInBounds(2, 2));
            Assert.IsTrue(_mapData.IsInBounds(1, 1));
        }

        [Test]
        public void IsInBounds_InvalidCoords_ReturnsFalse()
        {
            Assert.IsFalse(_mapData.IsInBounds(-1, 0));
            Assert.IsFalse(_mapData.IsInBounds(0, -1));
            Assert.IsFalse(_mapData.IsInBounds(3, 0));
            Assert.IsFalse(_mapData.IsInBounds(0, 3));
        }

        [Test]
        public void GetTileId_ValidCoords_ReturnsCorrectId()
        {
            Assert.AreEqual(0, _mapData.GetTileId(MapLayer.Ground, 0, 0));
            Assert.AreEqual(4, _mapData.GetTileId(MapLayer.Ground, 1, 1));
            Assert.AreEqual(7, _mapData.GetTileId(MapLayer.Ground, 1, 2));
            Assert.AreEqual(-1, _mapData.GetTileId(MapLayer.Ground, 2, 2));
        }

        [Test]
        public void GetTileId_RowMajorOrder_CorrectIndexing()
        {
            // (x=2, y=1) should be index 1*3+2 = 5
            Assert.AreEqual(5, _mapData.GetTileId(MapLayer.Ground, 2, 1));
            // (x=0, y=2) should be index 2*3+0 = 6
            Assert.AreEqual(6, _mapData.GetTileId(MapLayer.Ground, 0, 2));
        }

        [Test]
        public void GetTileId_OutOfBounds_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, _mapData.GetTileId(MapLayer.Ground, -1, 0));
            Assert.AreEqual(-1, _mapData.GetTileId(MapLayer.Ground, 3, 0));
        }

        [Test]
        public void GetTileId_NonexistentLayer_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, _mapData.GetTileId(MapLayer.Overlay, 0, 0));
        }

        [Test]
        public void SetTileId_UpdatesTile()
        {
            _mapData.SetTileId(MapLayer.Ground, 0, 0, 99);
            Assert.AreEqual(99, _mapData.GetTileId(MapLayer.Ground, 0, 0));
        }

        [Test]
        public void SetTileId_OutOfBounds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _mapData.SetTileId(MapLayer.Ground, -1, 0, 99));
            Assert.DoesNotThrow(() => _mapData.SetTileId(MapLayer.Ground, 99, 99, 99));
        }

        [Test]
        public void GetLayerData_ExistingLayer_ReturnsData()
        {
            var data = _mapData.GetLayerData(MapLayer.Ground);
            Assert.IsTrue(data.HasValue);
            Assert.AreEqual(MapLayer.Ground, data.Value.layer);
        }

        [Test]
        public void GetLayerData_MissingLayer_ReturnsNull()
        {
            Assert.IsFalse(_mapData.GetLayerData(MapLayer.Overlay).HasValue);
        }

        [Test]
        public void Dimensions_AreCorrect()
        {
            Assert.AreEqual(3, _mapData.Width);
            Assert.AreEqual(3, _mapData.Height);
        }
    }
}
