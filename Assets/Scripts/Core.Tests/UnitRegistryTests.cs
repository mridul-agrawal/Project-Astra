using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class UnitRegistryTests
    {
        private UnitRegistry _registry;
        private TestUnit _unit1;
        private TestUnit _unit2;
        private TestUnit _unit3;

        [SetUp]
        public void SetUp()
        {
            _registry = new UnitRegistry();
            _unit1 = new GameObject("Unit1").AddComponent<TestUnit>();
            _unit2 = new GameObject("Unit2").AddComponent<TestUnit>();
            _unit3 = new GameObject("Unit3").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_unit1.gameObject);
            Object.DestroyImmediate(_unit2.gameObject);
            Object.DestroyImmediate(_unit3.gameObject);
        }

        [Test]
        public void Register_IncreasesCount()
        {
            _registry.Register(_unit1, Faction.Player);
            Assert.AreEqual(1, _registry.UnitCount);
        }

        [Test]
        public void Register_DuplicateIgnored()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit1, Faction.Player);
            Assert.AreEqual(1, _registry.UnitCount);
        }

        [Test]
        public void Unregister_RemovesUnit()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Unregister(_unit1);
            Assert.AreEqual(0, _registry.UnitCount);
        }

        [Test]
        public void GetUnitsForFaction_ReturnsCorrectFaction()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Enemy);
            _registry.Register(_unit3, Faction.Player);

            var players = _registry.GetUnitsForFaction(Faction.Player);
            Assert.AreEqual(2, players.Count);
            Assert.Contains(_unit1, players);
            Assert.Contains(_unit3, players);
        }

        [Test]
        public void CanAct_TrueByDefault()
        {
            _registry.Register(_unit1, Faction.Player);
            Assert.IsTrue(_registry.CanAct(_unit1));
        }

        [Test]
        public void MarkActed_SetsCanActFalse()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.MarkActed(_unit1);
            Assert.IsFalse(_registry.CanAct(_unit1));
        }

        [Test]
        public void MarkActed_FiresEvent()
        {
            _registry.Register(_unit1, Faction.Player);
            TestUnit actedUnit = null;
            _registry.OnUnitActed += u => actedUnit = u;

            _registry.MarkActed(_unit1);
            Assert.AreEqual(_unit1, actedUnit);
        }

        [Test]
        public void GetActableUnits_ExcludesActed()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);
            _registry.MarkActed(_unit1);

            var actable = _registry.GetActableUnits(Faction.Player);
            Assert.AreEqual(1, actable.Count);
            Assert.AreEqual(_unit2, actable[0]);
        }

        [Test]
        public void ResetPhaseFlags_RestoresCanAct()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.MarkActed(_unit1);
            Assert.IsFalse(_registry.CanAct(_unit1));

            _registry.ResetPhaseFlags(Faction.Player);
            Assert.IsTrue(_registry.CanAct(_unit1));
        }

        [Test]
        public void ResetPhaseFlags_OnlyAffectsTargetFaction()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Enemy);
            _registry.MarkActed(_unit1);
            _registry.MarkActed(_unit2);

            _registry.ResetPhaseFlags(Faction.Player);
            Assert.IsTrue(_registry.CanAct(_unit1));
            Assert.IsFalse(_registry.CanAct(_unit2));
        }

        [Test]
        public void AllDone_TrueWhenAllActed()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);
            _registry.MarkActed(_unit1);
            _registry.MarkActed(_unit2);

            Assert.IsTrue(_registry.AllDone(Faction.Player));
        }

        [Test]
        public void AllDone_FalseWhenSomeRemain()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);
            _registry.MarkActed(_unit1);

            Assert.IsFalse(_registry.AllDone(Faction.Player));
        }

        [Test]
        public void AllDone_TrueForEmptyFaction()
        {
            Assert.IsTrue(_registry.AllDone(Faction.Enemy));
        }

        [Test]
        public void GetNextUnactedUnit_CyclesForward()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);
            _registry.Register(_unit3, Faction.Player);

            var next = _registry.GetNextUnactedUnit(Faction.Player, _unit1);
            Assert.AreEqual(_unit2, next);
        }

        [Test]
        public void GetNextUnactedUnit_WrapsAround()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);

            var next = _registry.GetNextUnactedUnit(Faction.Player, _unit2);
            Assert.AreEqual(_unit1, next);
        }

        [Test]
        public void GetNextUnactedUnit_SkipsActed()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Player);
            _registry.Register(_unit3, Faction.Player);
            _registry.MarkActed(_unit2);

            var next = _registry.GetNextUnactedUnit(Faction.Player, _unit1);
            Assert.AreEqual(_unit3, next);
        }

        [Test]
        public void GetFaction_ReturnsRegisteredFaction()
        {
            _registry.Register(_unit1, Faction.Enemy);
            Assert.AreEqual(Faction.Enemy, _registry.GetFaction(_unit1));
        }

        [Test]
        public void GetFaction_NullForUnregistered()
        {
            Assert.IsNull(_registry.GetFaction(_unit1));
        }

        [Test]
        public void HasUnitsOfFaction_TrueWhenPresent()
        {
            _registry.Register(_unit1, Faction.Allied);
            Assert.IsTrue(_registry.HasUnitsOfFaction(Faction.Allied));
        }

        [Test]
        public void HasUnitsOfFaction_FalseWhenAbsent()
        {
            _registry.Register(_unit1, Faction.Player);
            Assert.IsFalse(_registry.HasUnitsOfFaction(Faction.Allied));
        }

        [Test]
        public void Clear_RemovesAllUnits()
        {
            _registry.Register(_unit1, Faction.Player);
            _registry.Register(_unit2, Faction.Enemy);
            _registry.Clear();
            Assert.AreEqual(0, _registry.UnitCount);
        }
    }
}
