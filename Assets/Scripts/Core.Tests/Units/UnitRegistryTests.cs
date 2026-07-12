using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Units
{
    [TestFixture]
    public class UnitRegistryTests
    {
        private UnitRegistry registry;
        private TestUnit unit1;
        private TestUnit unit2;
        private TestUnit unit3;

        [SetUp]
        public void SetUp()
        {
            registry = new UnitRegistry();
            unit1 = new GameObject("Unit1").AddComponent<TestUnit>();
            unit2 = new GameObject("Unit2").AddComponent<TestUnit>();
            unit3 = new GameObject("Unit3").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(unit1.gameObject);
            Object.DestroyImmediate(unit2.gameObject);
            Object.DestroyImmediate(unit3.gameObject);
        }

        [Test]
        public void Register_IncreasesCount()
        {
            registry.Register(unit1, Faction.Player);
            Assert.AreEqual(1, registry.UnitCount);
        }

        [Test]
        public void Register_DuplicateIgnored()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit1, Faction.Player);
            Assert.AreEqual(1, registry.UnitCount);
        }

        [Test]
        public void Unregister_RemovesUnit()
        {
            registry.Register(unit1, Faction.Player);
            registry.Unregister(unit1);
            Assert.AreEqual(0, registry.UnitCount);
        }

        [Test]
        public void GetUnitsForFaction_ReturnsCorrectFaction()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Enemy);
            registry.Register(unit3, Faction.Player);

            var players = registry.GetUnitsForFaction(Faction.Player);
            Assert.AreEqual(2, players.Count);
            Assert.Contains(unit1, players);
            Assert.Contains(unit3, players);
        }

        [Test]
        public void CanAct_TrueByDefault()
        {
            registry.Register(unit1, Faction.Player);
            Assert.IsTrue(registry.CanAct(unit1));
        }

        [Test]
        public void MarkActed_SetsCanActFalse()
        {
            registry.Register(unit1, Faction.Player);
            registry.MarkActed(unit1);
            Assert.IsFalse(registry.CanAct(unit1));
        }

        [Test]
        public void MarkActed_FiresEvent()
        {
            registry.Register(unit1, Faction.Player);
            TestUnit actedUnit = null;
            registry.OnUnitActed += u => actedUnit = u;

            registry.MarkActed(unit1);
            Assert.AreEqual(unit1, actedUnit);
        }

        [Test]
        public void GetActableUnits_ExcludesActed()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);
            registry.MarkActed(unit1);

            var actable = registry.GetActableUnits(Faction.Player);
            Assert.AreEqual(1, actable.Count);
            Assert.AreEqual(unit2, actable[0]);
        }

        [Test]
        public void ResetPhaseFlags_RestoresCanAct()
        {
            registry.Register(unit1, Faction.Player);
            registry.MarkActed(unit1);
            Assert.IsFalse(registry.CanAct(unit1));

            registry.ResetPhaseFlags(Faction.Player);
            Assert.IsTrue(registry.CanAct(unit1));
        }

        [Test]
        public void ResetPhaseFlags_OnlyAffectsTargetFaction()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Enemy);
            registry.MarkActed(unit1);
            registry.MarkActed(unit2);

            registry.ResetPhaseFlags(Faction.Player);
            Assert.IsTrue(registry.CanAct(unit1));
            Assert.IsFalse(registry.CanAct(unit2));
        }

        [Test]
        public void AllDone_TrueWhenAllActed()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);
            registry.MarkActed(unit1);
            registry.MarkActed(unit2);

            Assert.IsTrue(registry.AllDone(Faction.Player));
        }

        [Test]
        public void AllDone_FalseWhenSomeRemain()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);
            registry.MarkActed(unit1);

            Assert.IsFalse(registry.AllDone(Faction.Player));
        }

        [Test]
        public void AllDone_TrueForEmptyFaction()
        {
            Assert.IsTrue(registry.AllDone(Faction.Enemy));
        }

        [Test]
        public void GetNextUnactedUnit_CyclesForward()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);
            registry.Register(unit3, Faction.Player);

            var next = registry.GetNextUnactedUnit(Faction.Player, unit1);
            Assert.AreEqual(unit2, next);
        }

        [Test]
        public void GetNextUnactedUnit_WrapsAround()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);

            var next = registry.GetNextUnactedUnit(Faction.Player, unit2);
            Assert.AreEqual(unit1, next);
        }

        [Test]
        public void GetNextUnactedUnit_SkipsActed()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Player);
            registry.Register(unit3, Faction.Player);
            registry.MarkActed(unit2);

            var next = registry.GetNextUnactedUnit(Faction.Player, unit1);
            Assert.AreEqual(unit3, next);
        }

        [Test]
        public void GetFaction_ReturnsRegisteredFaction()
        {
            registry.Register(unit1, Faction.Enemy);
            Assert.AreEqual(Faction.Enemy, registry.GetFaction(unit1));
        }

        [Test]
        public void GetFaction_NullForUnregistered()
        {
            Assert.IsNull(registry.GetFaction(unit1));
        }

        [Test]
        public void HasUnitsOfFaction_TrueWhenPresent()
        {
            registry.Register(unit1, Faction.Allied);
            Assert.IsTrue(registry.HasUnitsOfFaction(Faction.Allied));
        }

        [Test]
        public void HasUnitsOfFaction_FalseWhenAbsent()
        {
            registry.Register(unit1, Faction.Player);
            Assert.IsFalse(registry.HasUnitsOfFaction(Faction.Allied));
        }

        [Test]
        public void Clear_RemovesAllUnits()
        {
            registry.Register(unit1, Faction.Player);
            registry.Register(unit2, Faction.Enemy);
            registry.Clear();
            Assert.AreEqual(0, registry.UnitCount);
        }
    }
}
