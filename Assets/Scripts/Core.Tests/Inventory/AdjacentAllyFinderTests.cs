using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class AdjacentAllyFinderTests
    {
        private TestUnit center;
        private TestUnit north;
        private TestUnit east;
        private TestUnit enemy;

        [SetUp]
        public void SetUp()
        {
            center = CreateUnit("Center", new Vector2Int(3, 3), Faction.Player);
            north = CreateUnit("North", new Vector2Int(3, 4), Faction.Player);
            east = CreateUnit("East", new Vector2Int(4, 3), Faction.Player);
            enemy = CreateUnit("Enemy", new Vector2Int(3, 2), Faction.Enemy);
        }

        [TearDown]
        public void TearDown()
        {
            if (center != null) Object.DestroyImmediate(center.gameObject);
            if (north != null) Object.DestroyImmediate(north.gameObject);
            if (east != null) Object.DestroyImmediate(east.gameObject);
            if (enemy != null) Object.DestroyImmediate(enemy.gameObject);
        }

        private TestUnit Lookup(Vector2Int pos)
        {
            if (center.gridPosition == pos) return center;
            if (north.gridPosition == pos) return north;
            if (east.gridPosition == pos) return east;
            if (enemy.gridPosition == pos) return enemy;
            return null;
        }

        [Test]
        public void FindsAlliesAtCardinalOffsets()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                center.gridPosition, Faction.Player, center, Lookup);

            Assert.AreEqual(2, allies.Count);
            Assert.IsTrue(allies.Contains(north));
            Assert.IsTrue(allies.Contains(east));
        }

        [Test]
        public void ExcludesSelf()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                center.gridPosition, Faction.Player, center, Lookup);

            Assert.IsFalse(allies.Contains(center));
        }

        [Test]
        public void IgnoresEnemyFactionUnits()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                center.gridPosition, Faction.Player, center, Lookup);

            Assert.IsFalse(allies.Contains(enemy));
        }

        [Test]
        public void ReturnsEmptyListWhenNoAlliesAdjacent()
        {
            var isolated = CreateUnit("Isolated", new Vector2Int(10, 10), Faction.Player);
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                isolated.gridPosition, Faction.Player, isolated, Lookup);

            Assert.AreEqual(0, allies.Count);
            Object.DestroyImmediate(isolated.gameObject);
        }

        [Test]
        public void ReturnsMultipleAlliesIfSeveralAdjacent()
        {
            var south = CreateUnit("South", new Vector2Int(3, 2), Faction.Player);
            // Replace the enemy at (3,2) for this test
            Object.DestroyImmediate(enemy.gameObject);
            enemy = null;

            TestUnit LocalLookup(Vector2Int pos)
            {
                if (center.gridPosition == pos) return center;
                if (north.gridPosition == pos) return north;
                if (east.gridPosition == pos) return east;
                if (south.gridPosition == pos) return south;
                return null;
            }

            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                center.gridPosition, Faction.Player, center, LocalLookup);

            Assert.AreEqual(3, allies.Count);
            Object.DestroyImmediate(south.gameObject);
        }

        private static TestUnit CreateUnit(string name, Vector2Int pos, Faction faction)
        {
            var unit = new GameObject(name).AddComponent<TestUnit>();
            unit.faction = faction;
            unit.gridPosition = pos;
            return unit;
        }
    }
}
