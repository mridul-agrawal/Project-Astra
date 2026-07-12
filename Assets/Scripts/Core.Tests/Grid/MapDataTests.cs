using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Tests.Grid
{
    [TestFixture]
    public class MapDataTests
    {
        private MapData _mapData;

        [SetUp]
        public void SetUp()
        {
            _mapData = ScriptableObject.CreateInstance<MapData>();

            var so = new UnityEditor.SerializedObject(_mapData);
            so.FindProperty("_width").intValue = 3;
            so.FindProperty("_height").intValue = 3;
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
        public void Dimensions_AreCorrect()
        {
            Assert.AreEqual(3, _mapData.Width);
            Assert.AreEqual(3, _mapData.Height);
        }
    }
}
