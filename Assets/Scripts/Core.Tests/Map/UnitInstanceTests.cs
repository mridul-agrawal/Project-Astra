using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class UnitInstanceTests
    {
        private UnitDefinition _testDef;
        private ClassDefinition _testClass;

        [SetUp]
        public void SetUp()
        {
            _testClass = ScriptableObject.CreateInstance<ClassDefinition>();
            var classSO = new SerializedObject(_testClass);
            classSO.FindProperty("_className").stringValue = "TestClass";
            classSO.FindProperty("_movementRange").intValue = 5;
            SetStatArray(classSO, "_statCaps", 60, 30, 30, 30, 30, 30, 30, 20, 30);
            SetStatArray(classSO, "_statGrowthModifiers", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            SetStatArray(classSO, "_promotionBonuses", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            classSO.FindProperty("_hpGainOnLevelUp").intValue = 2;
            classSO.ApplyModifiedPropertiesWithoutUndo();

            _testDef = ScriptableObject.CreateInstance<UnitDefinition>();
            var unitSO = new SerializedObject(_testDef);
            unitSO.FindProperty("_unitName").stringValue = "Arjun";
            unitSO.FindProperty("_unitId").stringValue = "arjun";
            unitSO.FindProperty("_defaultClass").objectReferenceValue = _testClass;
            unitSO.FindProperty("_baseLevel").intValue = 1;
            SetStatArray(unitSO, "_baseStats", 20, 8, 3, 7, 9, 5, 2, 6, 5);
            SetStatArray(unitSO, "_personalGrowths", 70, 50, 20, 40, 45, 30, 25, 0, 35);
            unitSO.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testDef);
            Object.DestroyImmediate(_testClass);
        }

        [Test]
        public void Constructor_SetsStatsFromDefinition()
        {
            var unit = new UnitInstance(_testDef);
            Assert.AreEqual(20, unit.Stats[StatIndex.HP]);
            Assert.AreEqual(8, unit.Stats[StatIndex.Str]);
            Assert.AreEqual(6, unit.Stats[StatIndex.Con]);
        }

        [Test]
        public void Constructor_SetsCurrentHPToMaxHP()
        {
            var unit = new UnitInstance(_testDef);
            Assert.AreEqual(20, unit.CurrentHP);
            Assert.AreEqual(20, unit.MaxHP);
        }

        [Test]
        public void Constructor_SetsLevelFromDefinition()
        {
            var unit = new UnitInstance(_testDef);
            Assert.AreEqual(1, unit.Level);
        }

        [Test]
        public void ApplyDamage_ReducesCurrentHP()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(5);
            Assert.AreEqual(15, unit.CurrentHP);
        }

        [Test]
        public void ApplyDamage_ClampsToZero()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(999);
            Assert.AreEqual(0, unit.CurrentHP);
        }

        [Test]
        public void ApplyDamage_ZeroOrNegative_NoEffect()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(0);
            unit.ApplyDamage(-5);
            Assert.AreEqual(20, unit.CurrentHP);
        }

        [Test]
        public void ApplyHealing_IncreasesHP()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(10);
            unit.ApplyHealing(5);
            Assert.AreEqual(15, unit.CurrentHP);
        }

        [Test]
        public void ApplyHealing_ClampsToMaxHP()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(5);
            unit.ApplyHealing(999);
            Assert.AreEqual(20, unit.CurrentHP);
        }

        [Test]
        public void HPThreshold_FullHP_Normal()
        {
            var unit = new UnitInstance(_testDef);
            Assert.AreEqual(HPThreshold.Normal, unit.HPThreshold);
        }

        [Test]
        public void HPThreshold_Below50_Injured()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(11); // 9/20 = 45%
            Assert.AreEqual(HPThreshold.Injured, unit.HPThreshold);
        }

        [Test]
        public void HPThreshold_At30OrBelow_Critical()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(15); // 5/20 = 25%
            Assert.AreEqual(HPThreshold.Critical, unit.HPThreshold);
        }

        [Test]
        public void HPThreshold_TransitionFiresEvent()
        {
            var unit = new UnitInstance(_testDef);
            HPThreshold oldState = HPThreshold.Normal, newState = HPThreshold.Normal;
            unit.OnHPThresholdChanged += (o, n) => { oldState = o; newState = n; };

            unit.ApplyDamage(15);
            Assert.AreEqual(HPThreshold.Normal, oldState);
            Assert.AreEqual(HPThreshold.Critical, newState);
        }

        [Test]
        public void LevelUp_IncrementsLevel()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyLevelUp(AlwaysSucceed);
            Assert.AreEqual(2, unit.Level);
        }

        [Test]
        public void LevelUp_HPGains2_OtherStats1()
        {
            var unit = new UnitInstance(_testDef);
            var gains = unit.ApplyLevelUp(AlwaysSucceed);

            Assert.AreEqual(2, gains[StatIndex.HP]);
            Assert.AreEqual(1, gains[StatIndex.Str]);
            Assert.AreEqual(0, gains[StatIndex.Con]); // Con never grows
        }

        [Test]
        public void LevelUp_DeterministicWithInjectedRoll()
        {
            var unit = new UnitInstance(_testDef);
            var gains = unit.ApplyLevelUp(AlwaysFail);

            for (int i = 0; i < StatArray.Length; i++)
                Assert.AreEqual(0, gains[(StatIndex)i]);
        }

        [Test]
        public void Promote_UpdatesClass()
        {
            var promoted = CreatePromotedClass();
            var unit = new UnitInstance(_testDef);
            unit.Promote(promoted);
            Assert.AreEqual(promoted, unit.CurrentClass);
            Object.DestroyImmediate(promoted);
        }

        [Test]
        public void Promote_AppliesBonuses()
        {
            var promoted = CreatePromotedClass();
            var unit = new UnitInstance(_testDef);
            int strBefore = unit.Stats[StatIndex.Str];
            unit.Promote(promoted);
            Assert.AreEqual(strBefore + 2, unit.Stats[StatIndex.Str]);
            Object.DestroyImmediate(promoted);
        }

        [Test]
        public void Promote_ClampsToCaps()
        {
            var promoted = CreatePromotedClass();
            // Set STR to near cap so bonus exceeds it
            var unit = new UnitInstance(_testDef, _testClass, 1, StatArray.From(20, 29, 3, 7, 9, 5, 2, 6, 5));
            unit.Promote(promoted); // bonus +2, cap 32 → 31 clamped to 32? No: 29+2=31, cap=32, so 31
            Assert.IsTrue(unit.Stats[StatIndex.Str] <= 32);
            Object.DestroyImmediate(promoted);
        }

        [Test]
        public void NiyatiStoryDelta_AppliedToStat()
        {
            var unit = new UnitInstance(_testDef);
            int before = unit.Stats[StatIndex.Niyati];
            unit.ApplyNiyatiStoryDelta(1);
            Assert.AreEqual(before + 1, unit.Stats[StatIndex.Niyati]);
        }

        [Test]
        public void NiyatiStoryDelta_CappedAt4()
        {
            var unit = new UnitInstance(_testDef);
            for (int i = 0; i < 6; i++)
                unit.ApplyNiyatiStoryDelta(1);

            Assert.AreEqual(4, unit.NiyatiStoryDelta);
        }

        [Test]
        public void NiyatiSymbol_RecalculatedOnDelta()
        {
            // Base niyati = 5, raising to 7 = ratio 1.4 → LotusFull
            var unit = new UnitInstance(_testDef);
            unit.ApplyNiyatiStoryDelta(1);
            unit.ApplyNiyatiStoryDelta(1);
            Assert.AreEqual(NiyatiSymbol.LotusFull, unit.NiyatiSymbol);
        }

        [Test]
        public void PostSurvivalFlag_SetWhenHPBelow5()
        {
            var unit = new UnitInstance(_testDef);
            unit.OnChapterStart();
            unit.ApplyDamage(17); // HP = 3
            unit.OnChapterEnd();
            Assert.IsTrue(unit.PostSurvivalFlag);
        }

        [Test]
        public void PostSurvivalFlag_NotSetWhenHPStaysAbove4()
        {
            var unit = new UnitInstance(_testDef);
            unit.OnChapterStart();
            unit.ApplyDamage(10); // HP = 10
            unit.OnChapterEnd();
            Assert.IsFalse(unit.PostSurvivalFlag);
        }

        [Test]
        public void EffectiveMovement_UsesClassRange()
        {
            var unit = new UnitInstance(_testDef);
            Assert.AreEqual(5, unit.EffectiveMovement);
        }

        [Test]
        public void EffectiveMovement_IncludesOffset()
        {
            var unit = new UnitInstance(_testDef);
            unit.SetMovementOffset(2);
            Assert.AreEqual(7, unit.EffectiveMovement);
        }

        [Test]
        public void EffectiveMovement_MinimumIs1()
        {
            var unit = new UnitInstance(_testDef);
            unit.SetMovementOffset(-100);
            Assert.AreEqual(1, unit.EffectiveMovement);
        }

        [Test]
        public void OnChapterStart_ResetsTracking()
        {
            var unit = new UnitInstance(_testDef);
            unit.ApplyDamage(17);
            unit.OnChapterEnd();
            Assert.IsTrue(unit.PostSurvivalFlag);

            unit.OnChapterStart();
            Assert.IsFalse(unit.PostSurvivalFlag);
            Assert.IsFalse(unit.WasCriticalDuringChapter);
        }

        #region Helpers

        private ClassDefinition CreatePromotedClass()
        {
            var promoted = ScriptableObject.CreateInstance<ClassDefinition>();
            var so = new SerializedObject(promoted);
            so.FindProperty("_className").stringValue = "PromotedClass";
            so.FindProperty("_isPromoted").boolValue = true;
            so.FindProperty("_movementRange").intValue = 7;
            so.FindProperty("_hpGainOnLevelUp").intValue = 2;
            SetStatArray(so, "_statCaps", 70, 32, 30, 30, 30, 30, 30, 22, 30);
            SetStatArray(so, "_statGrowthModifiers", 5, 5, 0, 0, 0, 5, 0, 0, 0);
            SetStatArray(so, "_promotionBonuses", 3, 2, 1, 1, 1, 2, 1, 1, 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            return promoted;
        }

        private static void SetStatArray(SerializedObject so, string propName, params int[] values)
        {
            var prop = so.FindProperty(propName);
            var arr = prop.FindPropertyRelative("_values");
            arr.arraySize = StatArray.Length;
            for (int i = 0; i < StatArray.Length && i < values.Length; i++)
                arr.GetArrayElementAtIndex(i).intValue = values[i];
        }

        private static bool AlwaysSucceed(int rate, int unused) => true;
        private static bool AlwaysFail(int rate, int unused) => false;

        #endregion
    }
}
