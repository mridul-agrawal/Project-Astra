using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Tests.Grid
{
    [TestFixture]
    public class WaterBandTests
    {
        private MapData map;

        [TearDown]
        public void TearDown()
        {
            if (map != null) Object.DestroyImmediate(map);
        }

        private void MakeMap(int width, int height, Vector2Int[] waterCells)
        {
            map = ScriptableObject.CreateInstance<MapData>();
            var so = new SerializedObject(map);
            so.FindProperty("width").intValue = width;
            so.FindProperty("height").intValue = height;

            SerializedProperty terrain = so.FindProperty("terrain");
            terrain.arraySize = width * height;
            for (int i = 0; i < width * height; i++)
                terrain.GetArrayElementAtIndex(i).enumValueIndex = (int)TerrainType.Plain;
            foreach (Vector2Int c in waterCells)
                terrain.GetArrayElementAtIndex(c.y * width + c.x).enumValueIndex = (int)TerrainType.River;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Detect_SingleRowBand()
        {
            MakeMap(4, 4, new[] { new Vector2Int(0, 2), new Vector2Int(3, 2) });
            WaterBand band = WaterBand.Detect(map);
            Assert.AreEqual(2, band.MinY);
            Assert.AreEqual(2, band.MaxY);
            Assert.AreEqual(1, band.Height);
        }

        [Test]
        public void Detect_MultiRowBand()
        {
            MakeMap(4, 6, new[] { new Vector2Int(1, 2), new Vector2Int(2, 4) });
            WaterBand band = WaterBand.Detect(map);
            Assert.AreEqual(2, band.MinY);
            Assert.AreEqual(4, band.MaxY);
            Assert.AreEqual(3, band.Height);
        }

        [Test]
        public void Detect_NoWater_FallsBackToMiddleRow()
        {
            MakeMap(4, 8, new Vector2Int[0]);
            WaterBand band = WaterBand.Detect(map);
            Assert.AreEqual(4, band.MinY);
            Assert.AreEqual(4, band.MaxY);
        }
    }
}
