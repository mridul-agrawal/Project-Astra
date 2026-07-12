using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class StatBoosterTests
    {
        private UnitDefinition testDef;
        private ClassDefinition testClass;
        private TestUnit unit;

        [SetUp]
        public void SetUp()
        {
            testClass = ScriptableObject.CreateInstance<ClassDefinition>();
            var classSO = new SerializedObject(testClass);
            classSO.FindProperty("className").stringValue = "TestClass";
            classSO.FindProperty("movementRange").intValue = 5;
            SetStatArray(classSO, "statCaps", 60, 30, 30, 30, 30, 30, 30, 20, 30);
            SetStatArray(classSO, "statGrowthModifiers", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            SetStatArray(classSO, "promotionBonuses", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            classSO.FindProperty("hpGainOnLevelUp").intValue = 2;
            classSO.ApplyModifiedPropertiesWithoutUndo();

            testDef = ScriptableObject.CreateInstance<UnitDefinition>();
            var unitSO = new SerializedObject(testDef);
            unitSO.FindProperty("unitName").stringValue = "Arjun";
            unitSO.FindProperty("unitId").stringValue = "arjun";
            unitSO.FindProperty("defaultClass").objectReferenceValue = testClass;
            unitSO.FindProperty("baseLevel").intValue = 1;
            SetStatArray(unitSO, "baseStats", 20, 8, 3, 7, 9, 5, 2, 6, 5);
            SetStatArray(unitSO, "personalGrowths", 70, 50, 20, 40, 45, 30, 25, 0, 35);
            unitSO.ApplyModifiedPropertiesWithoutUndo();

            unit = new GameObject("BoosterTestUnit").AddComponent<TestUnit>();
            unit.BindUnitInstance(new UnitInstance(testDef));
        }

        [TearDown]
        public void TearDown()
        {
            if (unit != null) Object.DestroyImmediate(unit.gameObject);
            Object.DestroyImmediate(testDef);
            Object.DestroyImmediate(testClass);
        }

        #region UnitInstance.ApplyStatBoost

        [Test]
        public void ApplyStatBoost_IncreasesStatByAmount()
        {
            int before = unit.UnitInstance.Stats[StatIndex.Str];
            unit.UnitInstance.ApplyStatBoost(StatIndex.Str, 2);
            Assert.AreEqual(before + 2, unit.UnitInstance.Stats[StatIndex.Str]);
        }

        [Test]
        public void ApplyStatBoost_ClampsToClassCap()
        {
            // Str cap is 30, set to 29 then boost by 2 → should be 30
            var updated = unit.UnitInstance.Stats;
            updated[StatIndex.Str] = 29;
            // Use the 4-param constructor to set stats directly
            var inst = new UnitInstance(testDef, testClass, 1, updated);
            inst.ApplyStatBoost(StatIndex.Str, 2);
            Assert.AreEqual(30, inst.Stats[StatIndex.Str]);
        }

        [Test]
        public void ApplyStatBoost_AtCap_NoChange()
        {
            var updated = unit.UnitInstance.Stats;
            updated[StatIndex.Str] = 30;
            var inst = new UnitInstance(testDef, testClass, 1, updated);
            inst.ApplyStatBoost(StatIndex.Str, 2);
            Assert.AreEqual(30, inst.Stats[StatIndex.Str]);
        }

        [Test]
        public void ApplyStatBoost_HP_IncreasesMaxHP_CurrentHPUnchanged()
        {
            int maxBefore = unit.UnitInstance.MaxHP;
            int currentBefore = unit.UnitInstance.CurrentHP;
            unit.UnitInstance.ApplyStatBoost(StatIndex.HP, 7);
            Assert.AreEqual(maxBefore + 7, unit.UnitInstance.MaxHP);
            Assert.AreEqual(currentBefore, unit.UnitInstance.CurrentHP);
        }

        #endregion

        #region ConsumableEffects

        [Test]
        public void ConsumableEffects_StatBooster_ReturnsTrue()
        {
            var booster = ConsumableData.ShaktiMudrika;
            bool ok = ConsumableEffects.Apply(booster, unit, out string fail);
            Assert.IsTrue(ok, fail);
        }

        [Test]
        public void ConsumableEffects_StatBooster_NoUnitInstance_Fails()
        {
            var bare = new GameObject("BareUnit").AddComponent<TestUnit>();
            var booster = ConsumableData.ShaktiMudrika;
            bool ok = ConsumableEffects.Apply(booster, bare, out string fail);
            Assert.IsFalse(ok);
            Assert.IsNotNull(fail);
            Object.DestroyImmediate(bare.gameObject);
        }

        #endregion

        #region DescribeStatBoost

        [Test]
        public void DescribeStatBoost_NormalCase_NoReducedEffect()
        {
            var booster = ConsumableData.ShaktiMudrika;
            var (msg, reduced) = ConsumableEffects.DescribeStatBoost(booster, unit);
            Assert.IsFalse(reduced);
            Assert.IsTrue(msg.Contains("Strength"));
            Assert.IsTrue(msg.Contains("2"));
        }

        [Test]
        public void DescribeStatBoost_NearCap_ReducedEffectWarning()
        {
            var updated = unit.UnitInstance.Stats;
            updated[StatIndex.Str] = 29;
            unit.BindUnitInstance(new UnitInstance(testDef, testClass, 1, updated));

            var booster = ConsumableData.ShaktiMudrika;
            var (msg, reduced) = ConsumableEffects.DescribeStatBoost(booster, unit);
            Assert.IsTrue(reduced);
            Assert.IsTrue(msg.Contains("reduced"));
        }

        [Test]
        public void DescribeStatBoost_AtCap_NoEffectWarning()
        {
            var updated = unit.UnitInstance.Stats;
            updated[StatIndex.Str] = 30;
            unit.BindUnitInstance(new UnitInstance(testDef, testClass, 1, updated));

            var booster = ConsumableData.ShaktiMudrika;
            var (msg, reduced) = ConsumableEffects.DescribeStatBoost(booster, unit);
            Assert.IsTrue(reduced);
            Assert.IsTrue(msg.Contains("no effect"));
        }

        #endregion

        #region Booster factories

        [Test]
        public void AllNineBoosterFactories_HaveCorrectStatAndMagnitude()
        {
            AssertBooster(ConsumableData.AmritaVastra, StatIndex.HP, 7);
            AssertBooster(ConsumableData.ShaktiMudrika, StatIndex.Str, 2);
            AssertBooster(ConsumableData.VidyaDhuli, StatIndex.Mag, 2);
            AssertBooster(ConsumableData.SutraGrantha, StatIndex.Skl, 2);
            AssertBooster(ConsumableData.VayuPankha, StatIndex.Spd, 2);
            AssertBooster(ConsumableData.DeviPratima, StatIndex.Niyati, 2);
            AssertBooster(ConsumableData.NagaKavach, StatIndex.Def, 2);
            AssertBooster(ConsumableData.RakshaSutra, StatIndex.Res, 2);
            AssertBooster(ConsumableData.DehaMudrika, StatIndex.Con, 2);
        }

        private static void AssertBooster(ConsumableData booster, StatIndex expectedStat, int expectedMagnitude)
        {
            Assert.AreEqual(ConsumableType.StatBooster, booster.type, $"Type mismatch for {booster.name}");
            Assert.AreEqual(expectedStat, booster.targetStat, $"Stat mismatch for {booster.name}");
            Assert.AreEqual(expectedMagnitude, booster.magnitude, $"Magnitude mismatch for {booster.name}");
            Assert.AreEqual(1, booster.maxUses, $"Uses mismatch for {booster.name}");
            Assert.AreEqual(1, booster.currentUses, $"Current uses mismatch for {booster.name}");
            Assert.IsFalse(string.IsNullOrEmpty(booster.name), "Booster must have a name");
        }

        #endregion

        #region Integration: inventory use

        [Test]
        public void TryUseConsumable_StatBooster_AppliesAndRemovesSlot()
        {
            int strBefore = unit.UnitInstance.Stats[StatIndex.Str];
            unit.Inventory.TryAddItem(InventoryItem.FromConsumable(ConsumableData.ShaktiMudrika), out int slot);

            bool ok = unit.Inventory.TryUseConsumable(slot, out string fail);

            Assert.IsTrue(ok, fail);
            Assert.AreEqual(strBefore + 2, unit.UnitInstance.Stats[StatIndex.Str]);
            Assert.IsTrue(unit.Inventory.GetSlot(slot).IsEmpty);
        }

        #endregion

        private static void SetStatArray(SerializedObject so, string propName, params int[] values)
        {
            var prop = so.FindProperty(propName);
            var arr = prop.FindPropertyRelative("values");
            arr.arraySize = StatArray.Length;
            for (int i = 0; i < StatArray.Length && i < values.Length; i++)
                arr.GetArrayElementAtIndex(i).intValue = values[i];
        }
    }
}
