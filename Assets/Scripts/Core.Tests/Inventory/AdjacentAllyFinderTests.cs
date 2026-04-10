using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class AdjacentAllyFinderTests
    {
        private TestUnit _center;
        private TestUnit _north;
        private TestUnit _east;
        private TestUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _center = CreateUnit("Center", new Vector2Int(3, 3), Faction.Player);
            _north = CreateUnit("North", new Vector2Int(3, 4), Faction.Player);
            _east = CreateUnit("East", new Vector2Int(4, 3), Faction.Player);
            _enemy = CreateUnit("Enemy", new Vector2Int(3, 2), Faction.Enemy);
        }

        [TearDown]
        public void TearDown()
        {
            if (_center != null) Object.DestroyImmediate(_center.gameObject);
            if (_north != null) Object.DestroyImmediate(_north.gameObject);
            if (_east != null) Object.DestroyImmediate(_east.gameObject);
            if (_enemy != null) Object.DestroyImmediate(_enemy.gameObject);
        }

        private TestUnit Lookup(Vector2Int pos)
        {
            if (_center.gridPosition == pos) return _center;
            if (_north.gridPosition == pos) return _north;
            if (_east.gridPosition == pos) return _east;
            if (_enemy.gridPosition == pos) return _enemy;
            return null;
        }

        [Test]
        public void FindsAlliesAtCardinalOffsets()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                _center.gridPosition, Faction.Player, _center, Lookup);

            Assert.AreEqual(2, allies.Count);
            Assert.IsTrue(allies.Contains(_north));
            Assert.IsTrue(allies.Contains(_east));
        }

        [Test]
        public void ExcludesSelf()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                _center.gridPosition, Faction.Player, _center, Lookup);

            Assert.IsFalse(allies.Contains(_center));
        }

        [Test]
        public void IgnoresEnemyFactionUnits()
        {
            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                _center.gridPosition, Faction.Player, _center, Lookup);

            Assert.IsFalse(allies.Contains(_enemy));
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
            Object.DestroyImmediate(_enemy.gameObject);
            _enemy = null;

            TestUnit LocalLookup(Vector2Int pos)
            {
                if (_center.gridPosition == pos) return _center;
                if (_north.gridPosition == pos) return _north;
                if (_east.gridPosition == pos) return _east;
                if (south.gridPosition == pos) return south;
                return null;
            }

            var allies = AdjacentAllyFinder.FindAdjacentAllies(
                _center.gridPosition, Faction.Player, _center, LocalLookup);

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
