using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Tests.Grid
{
    [TestFixture]
    public class MapDataTests
    {
        private MapData mapData;

        [SetUp]
        public void SetUp()
        {
            mapData = ScriptableObject.CreateInstance<MapData>();

            var so = new UnityEditor.SerializedObject(mapData);
            so.FindProperty("width").intValue = 3;
            so.FindProperty("height").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mapData);
        }

        [Test]
        public void IsInBounds_ValidCoords_ReturnsTrue()
        {
            Assert.IsTrue(mapData.IsInBounds(0, 0));
            Assert.IsTrue(mapData.IsInBounds(2, 2));
            Assert.IsTrue(mapData.IsInBounds(1, 1));
        }

        [Test]
        public void IsInBounds_InvalidCoords_ReturnsFalse()
        {
            Assert.IsFalse(mapData.IsInBounds(-1, 0));
            Assert.IsFalse(mapData.IsInBounds(0, -1));
            Assert.IsFalse(mapData.IsInBounds(3, 0));
            Assert.IsFalse(mapData.IsInBounds(0, 3));
        }

        [Test]
        public void Dimensions_AreCorrect()
        {
            Assert.AreEqual(3, mapData.Width);
            Assert.AreEqual(3, mapData.Height);
        }
    }
}
