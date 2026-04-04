using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class TerrainStatTableTests
    {
        private TerrainStatTable _table;

        [SetUp]
        public void SetUp()
        {
            _table = ScriptableObject.CreateInstance<TerrainStatTable>();

            var so = new UnityEditor.SerializedObject(_table);
            var statsProp = so.FindProperty("_stats");
            statsProp.arraySize = 18;

            // Set up Plain (index 0)
            var plain = statsProp.GetArrayElementAtIndex((int)TerrainType.Plain);
            plain.FindPropertyRelative("moveCostFoot").intValue = 1;
            plain.FindPropertyRelative("moveCostMounted").intValue = 1;
            plain.FindPropertyRelative("moveCostArmoured").intValue = 1;
            plain.FindPropertyRelative("moveCostFlying").intValue = 1;
            plain.FindPropertyRelative("moveCostPirate").intValue = 1;
            plain.FindPropertyRelative("defenceBonus").intValue = 0;
            plain.FindPropertyRelative("avoidBonus").intValue = 0;

            // Set up Forest (index 1)
            var forest = statsProp.GetArrayElementAtIndex((int)TerrainType.Forest);
            forest.FindPropertyRelative("moveCostFoot").intValue = 2;
            forest.FindPropertyRelative("moveCostMounted").intValue = 3;
            forest.FindPropertyRelative("moveCostArmoured").intValue = 3;
            forest.FindPropertyRelative("moveCostFlying").intValue = 1;
            forest.FindPropertyRelative("moveCostPirate").intValue = 2;
            forest.FindPropertyRelative("defenceBonus").intValue = 1;
            forest.FindPropertyRelative("avoidBonus").intValue = 20;

            // Set up Fort (index 9)
            var fort = statsProp.GetArrayElementAtIndex((int)TerrainType.Fort);
            fort.FindPropertyRelative("moveCostFoot").intValue = 1;
            fort.FindPropertyRelative("moveCostMounted").intValue = 1;
            fort.FindPropertyRelative("moveCostArmoured").intValue = 1;
            fort.FindPropertyRelative("moveCostFlying").intValue = 1;
            fort.FindPropertyRelative("moveCostPirate").intValue = 1;
            fort.FindPropertyRelative("defenceBonus").intValue = 2;
            fort.FindPropertyRelative("avoidBonus").intValue = 20;
            fort.FindPropertyRelative("healPerTurn").intValue = 10;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
        }

        [Test]
        public void GetStats_Plain_ReturnsCorrectValues()
        {
            var stats = _table.GetStats(TerrainType.Plain);
            Assert.AreEqual(1, stats.moveCostFoot);
            Assert.AreEqual(0, stats.defenceBonus);
            Assert.AreEqual(0, stats.avoidBonus);
        }

        [Test]
        public void GetStats_Forest_ReturnsCorrectBonuses()
        {
            var stats = _table.GetStats(TerrainType.Forest);
            Assert.AreEqual(2, stats.moveCostFoot);
            Assert.AreEqual(3, stats.moveCostMounted);
            Assert.AreEqual(1, stats.moveCostFlying);
            Assert.AreEqual(1, stats.defenceBonus);
            Assert.AreEqual(20, stats.avoidBonus);
        }

        [Test]
        public void GetStats_Fort_HasHealPerTurn()
        {
            var stats = _table.GetStats(TerrainType.Fort);
            Assert.AreEqual(10, stats.healPerTurn);
            Assert.AreEqual(2, stats.defenceBonus);
            Assert.AreEqual(20, stats.avoidBonus);
        }

        [Test]
        public void GetStats_InvalidTerrain_ReturnsDefault()
        {
            var stats = _table.GetStats((TerrainType)99);
            Assert.AreEqual(TerrainStats.Default.moveCostFoot, stats.moveCostFoot);
        }

        [Test]
        public void TerrainCount_Returns18()
        {
            Assert.AreEqual(18, _table.TerrainCount);
        }

        [Test]
        public void GetStats_AllTerrainTypes_DoNotThrow()
        {
            for (int i = 0; i < 18; i++)
            {
                Assert.DoesNotThrow(() => _table.GetStats((TerrainType)i));
            }
        }
    }
}
