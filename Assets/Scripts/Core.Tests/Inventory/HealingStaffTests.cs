using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class HealingStaffTests
    {
        private UnitDefinition healerDef;
        private UnitDefinition targetDef;
        private ClassDefinition healerClass;
        private ClassDefinition targetClass;
        private TestUnit healer;
        private TestUnit target;

        [SetUp]
        public void SetUp()
        {
            healerClass = ScriptableObject.CreateInstance<ClassDefinition>();
            var hcSO = new SerializedObject(healerClass);
            hcSO.FindProperty("className").stringValue = "Cleric";
            hcSO.FindProperty("movementRange").intValue = 5;
            SetStatArray(hcSO, "statCaps", 60, 30, 30, 30, 30, 30, 30, 20, 30);
            SetStatArray(hcSO, "statGrowthModifiers", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            SetStatArray(hcSO, "promotionBonuses", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            hcSO.FindProperty("hpGainOnLevelUp").intValue = 2;
            var whitelist = hcSO.FindProperty("weaponWhitelist");
            whitelist.arraySize = 1;
            whitelist.GetArrayElementAtIndex(0).enumValueIndex = (int)WeaponType.Staff;
            hcSO.ApplyModifiedPropertiesWithoutUndo();

            targetClass = ScriptableObject.CreateInstance<ClassDefinition>();
            var tcSO = new SerializedObject(targetClass);
            tcSO.FindProperty("className").stringValue = "Fighter";
            tcSO.FindProperty("movementRange").intValue = 5;
            SetStatArray(tcSO, "statCaps", 60, 30, 30, 30, 30, 30, 30, 20, 30);
            SetStatArray(tcSO, "statGrowthModifiers", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            SetStatArray(tcSO, "promotionBonuses", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            tcSO.FindProperty("hpGainOnLevelUp").intValue = 2;
            tcSO.ApplyModifiedPropertiesWithoutUndo();

            // Healer: HP=20, Mag=6
            healerDef = ScriptableObject.CreateInstance<UnitDefinition>();
            var hdSO = new SerializedObject(healerDef);
            hdSO.FindProperty("unitName").stringValue = "Priya";
            hdSO.FindProperty("unitId").stringValue = "priya";
            hdSO.FindProperty("defaultClass").objectReferenceValue = healerClass;
            hdSO.FindProperty("baseLevel").intValue = 1;
            SetStatArray(hdSO, "baseStats", 20, 3, 6, 5, 5, 3, 7, 4, 5);
            SetStatArray(hdSO, "personalGrowths", 50, 20, 60, 40, 40, 20, 50, 0, 35);
            hdSO.ApplyModifiedPropertiesWithoutUndo();

            // Target: HP=25
            targetDef = ScriptableObject.CreateInstance<UnitDefinition>();
            var tdSO = new SerializedObject(targetDef);
            tdSO.FindProperty("unitName").stringValue = "Arjun";
            tdSO.FindProperty("unitId").stringValue = "arjun";
            tdSO.FindProperty("defaultClass").objectReferenceValue = targetClass;
            tdSO.FindProperty("baseLevel").intValue = 1;
            SetStatArray(tdSO, "baseStats", 25, 8, 2, 7, 9, 5, 2, 6, 5);
            SetStatArray(tdSO, "personalGrowths", 70, 50, 20, 40, 45, 30, 25, 0, 35);
            tdSO.ApplyModifiedPropertiesWithoutUndo();

            healer = new GameObject("Healer").AddComponent<TestUnit>();
            healer.BindUnitInstance(new UnitInstance(healerDef));
            healer.faction = Faction.Player;
            healer.gridPosition = new Vector2Int(3, 3);

            target = new GameObject("Target").AddComponent<TestUnit>();
            target.BindUnitInstance(new UnitInstance(targetDef));
            target.faction = Faction.Player;
            target.gridPosition = new Vector2Int(3, 4);
        }

        [TearDown]
        public void TearDown()
        {
            if (healer != null) Object.DestroyImmediate(healer.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(healerDef);
            Object.DestroyImmediate(targetDef);
            Object.DestroyImmediate(healerClass);
            Object.DestroyImmediate(targetClass);
        }

        #region ComputeHealAmount

        [Test]
        public void ComputeHealAmount_StandardHeal_ReturnsMightPlusMag()
        {
            var staff = TestItems.Heal; // might=10
            int result = StaffEffects.ComputeHealAmount(staff, 6, 5, 25);
            Assert.AreEqual(16, result); // 10+6=16, missing 20
        }

        [Test]
        public void ComputeHealAmount_ClampsToMissingHP()
        {
            var staff = TestItems.Heal;
            int result = StaffEffects.ComputeHealAmount(staff, 6, 22, 25);
            Assert.AreEqual(3, result); // 10+6=16 but only 3 missing
        }

        [Test]
        public void ComputeHealAmount_FullHeal_RestoresToMax()
        {
            var staff = TestItems.Recover;
            int result = StaffEffects.ComputeHealAmount(staff, 6, 5, 25);
            Assert.AreEqual(20, result);
        }

        [Test]
        public void ComputeHealAmount_Ranged_HalvesMight()
        {
            var staff = TestItems.Physic; // might=10
            int result = StaffEffects.ComputeHealAmount(staff, 6, 5, 25);
            Assert.AreEqual(11, result); // 10/2 + 6 = 11, missing 20
        }

        [Test]
        public void ComputeHealAmount_AoE_HalvesMight()
        {
            var staff = TestItems.Fortify; // might=10
            int result = StaffEffects.ComputeHealAmount(staff, 6, 5, 25);
            Assert.AreEqual(11, result); // 10/2 + 6 = 11
        }

        [Test]
        public void ComputeHealAmount_TargetAtFullHP_ReturnsZero()
        {
            var staff = TestItems.Heal;
            int result = StaffEffects.ComputeHealAmount(staff, 6, 25, 25);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ComputeHealAmount_OddMight_RoundsDown()
        {
            var staff = TestItems.Physic;
            // might=10, 10/2=5, even. Make a staff with odd might.
            var oddStaff = staff;
            oddStaff.might = 7;
            int result = StaffEffects.ComputeHealAmount(oddStaff, 6, 5, 25);
            Assert.AreEqual(9, result); // 7/2=3 + 6 = 9
        }

        #endregion

        #region CanUseStaff validation

        [Test]
        public void CanUseStaff_BrokenStaff_Fails()
        {
            var staff = TestItems.Heal;
            staff.currentUses = 0;
            bool ok = StaffEffects.CanUseStaff(healer, staff, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("broken"));
        }

        [Test]
        public void CanUseStaff_NotStaff_Fails()
        {
            var sword = TestItems.IronSword;
            bool ok = StaffEffects.CanUseStaff(healer, sword, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("Not a healing staff"));
        }

        [Test]
        public void CanUseStaff_ValidStaff_Succeeds()
        {
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanUseStaff(healer, staff, out _);
            Assert.IsTrue(ok);
        }

        #endregion

        #region CanHealTarget validation

        [Test]
        public void CanHealTarget_SelfTarget_Fails()
        {
            healer.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanHealTarget(healer, healer, staff, 6, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("self-heal"));
        }

        [Test]
        public void CanHealTarget_FullHP_Fails()
        {
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanHealTarget(healer, target, staff, 6, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("full HP"));
        }

        [Test]
        public void CanHealTarget_EnemyFaction_Fails()
        {
            target.faction = Faction.Enemy;
            target.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanHealTarget(healer, target, staff, 6, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("allied"));
        }

        [Test]
        public void CanHealTarget_OutOfRange_Fails()
        {
            target.UnitInstance.ApplyDamage(5);
            target.gridPosition = new Vector2Int(10, 10); // far away
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanHealTarget(healer, target, staff, 6, out string fail);
            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("out of range"));
        }

        [Test]
        public void CanHealTarget_AlliedFaction_Succeeds()
        {
            target.faction = Faction.Allied;
            target.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Heal;
            bool ok = StaffEffects.CanHealTarget(healer, target, staff, 6, out _);
            Assert.IsTrue(ok);
        }

        #endregion

        #region ApplyHeal

        [Test]
        public void ApplyHeal_RestoresHP_ConsumesDurability()
        {
            target.UnitInstance.ApplyDamage(20);
            int hpBefore = target.UnitInstance.CurrentHP;
            var staff = TestItems.Heal;
            int usesBefore = staff.currentUses;

            bool ok = StaffEffects.ApplyHeal(healer, target, ref staff, out int healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(16, healed); // might(10) + mag(6), missing 20 so no clamping
            Assert.AreEqual(hpBefore + 16, target.UnitInstance.CurrentHP);
            Assert.AreEqual(usesBefore - 1, staff.currentUses);
        }

        [Test]
        public void ApplyHeal_LastUse_HealsFullyThenStaffBroken()
        {
            target.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Heal;
            staff.currentUses = 1;

            bool ok = StaffEffects.ApplyHeal(healer, target, ref staff, out int healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(5, healed); // clamped to missing HP
            Assert.AreEqual(target.UnitInstance.MaxHP, target.UnitInstance.CurrentHP);
            Assert.IsTrue(staff.IsBroken);
        }

        [Test]
        public void ApplyHeal_Recover_FullyRestores()
        {
            target.UnitInstance.ApplyDamage(20);
            var staff = TestItems.Recover;

            bool ok = StaffEffects.ApplyHeal(healer, target, ref staff, out int healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(20, healed);
            Assert.AreEqual(target.UnitInstance.MaxHP, target.UnitInstance.CurrentHP);
        }

        [Test]
        public void ApplyHeal_Physic_HalvedPowerAtRange()
        {
            target.UnitInstance.ApplyDamage(20);
            target.gridPosition = new Vector2Int(6, 3); // distance 3, within Mag/2=3
            var staff = TestItems.Physic;

            bool ok = StaffEffects.ApplyHeal(healer, target, ref staff, out int healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(11, healed); // 10/2 + 6 = 11
        }

        #endregion

        #region ApplyFortify

        [Test]
        public void ApplyFortify_HealsAllDamagedAlliesInRadius()
        {
            var ally2 = new GameObject("Ally2").AddComponent<TestUnit>();
            ally2.BindUnitInstance(new UnitInstance(targetDef));
            ally2.faction = Faction.Player;
            ally2.gridPosition = new Vector2Int(4, 3);

            target.UnitInstance.ApplyDamage(10);
            ally2.UnitInstance.ApplyDamage(8);

            var staff = TestItems.Fortify;
            var allUnits = new List<TestUnit> { healer, target, ally2 };

            bool ok = StaffEffects.ApplyFortify(healer, allUnits, ref staff, out var healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(2, healed.Count);

            Object.DestroyImmediate(ally2.gameObject);
        }

        [Test]
        public void ApplyFortify_SkipsFullHPAllies()
        {
            target.UnitInstance.ApplyDamage(10);
            var fullHPAlly = new GameObject("FullHP").AddComponent<TestUnit>();
            fullHPAlly.BindUnitInstance(new UnitInstance(targetDef));
            fullHPAlly.faction = Faction.Player;
            fullHPAlly.gridPosition = new Vector2Int(4, 3);

            var staff = TestItems.Fortify;
            var allUnits = new List<TestUnit> { target, fullHPAlly };

            bool ok = StaffEffects.ApplyFortify(healer, allUnits, ref staff, out var healed, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, healed.Count);
            Assert.AreEqual(target, healed[0].unit);

            Object.DestroyImmediate(fullHPAlly.gameObject);
        }

        [Test]
        public void ApplyFortify_IncludesCasterIfDamaged()
        {
            healer.UnitInstance.ApplyDamage(5);
            target.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Fortify;
            var allUnits = new List<TestUnit> { healer, target };

            bool ok = StaffEffects.ApplyFortify(healer, allUnits, ref staff, out var healed, out _);

            Assert.IsTrue(ok);
            bool healerWasHealed = healed.Exists(h => h.unit == healer);
            Assert.IsTrue(healerWasHealed);
        }

        [Test]
        public void ApplyFortify_ExcludesCasterIfFullHP()
        {
            target.UnitInstance.ApplyDamage(5);
            var staff = TestItems.Fortify;
            var allUnits = new List<TestUnit> { healer, target };

            bool ok = StaffEffects.ApplyFortify(healer, allUnits, ref staff, out var healed, out _);

            Assert.IsTrue(ok);
            bool healerWasHealed = healed.Exists(h => h.unit == healer);
            Assert.IsFalse(healerWasHealed);
        }

        [Test]
        public void ApplyFortify_ConsumesDurabilityOnce()
        {
            target.UnitInstance.ApplyDamage(5);
            var ally2 = new GameObject("Ally2").AddComponent<TestUnit>();
            ally2.BindUnitInstance(new UnitInstance(targetDef));
            ally2.faction = Faction.Player;
            ally2.gridPosition = new Vector2Int(4, 3);
            ally2.UnitInstance.ApplyDamage(5);

            var staff = TestItems.Fortify;
            int usesBefore = staff.currentUses;
            var allUnits = new List<TestUnit> { target, ally2 };

            StaffEffects.ApplyFortify(healer, allUnits, ref staff, out _, out _);

            Assert.AreEqual(usesBefore - 1, staff.currentUses);

            Object.DestroyImmediate(ally2.gameObject);
        }

        [Test]
        public void ApplyFortify_NoValidTargets_Fails()
        {
            var staff = TestItems.Fortify;
            var allUnits = new List<TestUnit> { target }; // target at full HP

            bool ok = StaffEffects.ApplyFortify(healer, allUnits, ref staff, out _, out string fail);

            Assert.IsFalse(ok);
            Assert.IsTrue(fail.Contains("No valid targets"));
        }

        #endregion

        #region StaffRangeResolver

        [Test]
        public void PhysicRange_EqualsMagDiv2_Min1()
        {
            var staff = TestItems.Physic;
            Assert.AreEqual(3, StaffRangeResolver.GetEffectiveMaxRange(staff, 6)); // 6/2=3
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMaxRange(staff, 1)); // min 1
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMaxRange(staff, 0)); // min 1
            Assert.AreEqual(5, StaffRangeResolver.GetEffectiveMaxRange(staff, 10)); // 10/2=5
        }

        [Test]
        public void FortifyRadius_EqualsMagDiv2_Min1()
        {
            var staff = TestItems.Fortify;
            Assert.AreEqual(3, StaffRangeResolver.GetEffectiveMaxRange(staff, 6));
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMaxRange(staff, 1));
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMaxRange(staff, 0));
        }

        [Test]
        public void StandardStaff_UsesStaticRange()
        {
            var staff = TestItems.Heal;
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMaxRange(staff, 20));
            Assert.AreEqual(1, StaffRangeResolver.GetEffectiveMinRange(staff));
        }

        [Test]
        public void FortifyMinRange_IsZero()
        {
            var staff = TestItems.Fortify;
            Assert.AreEqual(0, StaffRangeResolver.GetEffectiveMinRange(staff));
        }

        [Test]
        public void IsInRange_Adjacent_StandardStaff_True()
        {
            var staff = TestItems.Heal;
            Assert.IsTrue(StaffRangeResolver.IsInRange(
                staff, 6, new Vector2Int(3, 3), new Vector2Int(3, 4)));
        }

        [Test]
        public void IsInRange_TwoAway_StandardStaff_False()
        {
            var staff = TestItems.Heal;
            Assert.IsFalse(StaffRangeResolver.IsInRange(
                staff, 6, new Vector2Int(3, 3), new Vector2Int(3, 5)));
        }

        [Test]
        public void IsInRange_Physic_ExactBoundary_True()
        {
            var staff = TestItems.Physic;
            // mag=6, range=3. Distance of exactly 3 should be in range.
            Assert.IsTrue(StaffRangeResolver.IsInRange(
                staff, 6, new Vector2Int(3, 3), new Vector2Int(6, 3)));
        }

        [Test]
        public void IsInRange_Physic_BeyondBoundary_False()
        {
            var staff = TestItems.Physic;
            // mag=6, range=3. Distance of 4 should be out of range.
            Assert.IsFalse(StaffRangeResolver.IsInRange(
                staff, 6, new Vector2Int(3, 3), new Vector2Int(7, 3)));
        }

        #endregion

        #region Staff factories

        [Test]
        public void AllStaffFactories_HaveCorrectValues()
        {
            AssertStaff(TestItems.Heal, "Chikitsa", StaffEffect.Heal,
                might: 10, uses: 30, rank: WeaponRank.E, minRange: 1, maxRange: 1);

            AssertStaff(TestItems.Mend, "Sukhada", StaffEffect.Heal,
                might: 20, uses: 20, rank: WeaponRank.C, minRange: 1, maxRange: 1);

            AssertStaff(TestItems.Recover, "Kayakalpa", StaffEffect.FullHeal,
                might: 0, uses: 15, rank: WeaponRank.B, minRange: 1, maxRange: 1);

            AssertStaff(TestItems.Physic, "Dooradarshi", StaffEffect.Ranged,
                might: 10, uses: 15, rank: WeaponRank.B, minRange: 1, maxRange: 1);

            AssertStaff(TestItems.Fortify, "Sarva Raksha", StaffEffect.AreaOfEffect,
                might: 10, uses: 8, rank: WeaponRank.A, minRange: 0, maxRange: 0);
        }

        private static void AssertStaff(
            WeaponData staff, string expectedName, StaffEffect expectedEffect,
            int might, int uses, WeaponRank rank, int minRange, int maxRange)
        {
            Assert.AreEqual(expectedName, staff.name, $"Name mismatch for {expectedName}");
            Assert.AreEqual(WeaponType.Staff, staff.weaponType, $"Type mismatch for {expectedName}");
            Assert.AreEqual(DamageType.Magical, staff.damageType, $"DamageType mismatch for {expectedName}");
            Assert.AreEqual(expectedEffect, staff.staffEffect, $"StaffEffect mismatch for {expectedName}");
            Assert.AreEqual(might, staff.might, $"Might mismatch for {expectedName}");
            Assert.AreEqual(uses, staff.maxUses, $"MaxUses mismatch for {expectedName}");
            Assert.AreEqual(uses, staff.currentUses, $"CurrentUses mismatch for {expectedName}");
            Assert.AreEqual(rank, staff.minRank, $"Rank mismatch for {expectedName}");
            Assert.AreEqual(minRange, staff.minRange, $"MinRange mismatch for {expectedName}");
            Assert.AreEqual(maxRange, staff.maxRange, $"MaxRange mismatch for {expectedName}");
            Assert.AreEqual(100, staff.hit, $"Hit mismatch for {expectedName}");
            Assert.AreEqual(0, staff.crit, $"Crit mismatch for {expectedName}");
        }

        #endregion

        #region Inventory integration

        [Test]
        public void TryUseStaff_HealsAndUpdatesInventory()
        {
            healer.Inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.Heal), out _);
            target.UnitInstance.ApplyDamage(15);

            bool ok = healer.Inventory.TryUseStaff(target, out int healed, out string fail);

            Assert.IsTrue(ok, fail);
            Assert.AreEqual(15, healed); // clamped: missing 15, formula gives 16
            Assert.AreEqual(target.UnitInstance.MaxHP, target.UnitInstance.CurrentHP);
            Assert.AreEqual(29, healer.Inventory.GetSlot(0).weapon.currentUses);
        }

        [Test]
        public void TryUseStaff_LastUse_RemovesFromInventory()
        {
            var staff = TestItems.Heal;
            staff.currentUses = 1;
            healer.Inventory.TryAddItem(InventoryItem.FromWeapon(staff), out int slot);
            target.UnitInstance.ApplyDamage(5);

            bool ok = healer.Inventory.TryUseStaff(target, out _, out _);

            Assert.IsTrue(ok);
            Assert.IsTrue(healer.Inventory.GetSlot(slot).IsEmpty);
        }

        [Test]
        public void TryUseStaff_NonStaffEquipped_Fails()
        {
            healer.Inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            target.UnitInstance.ApplyDamage(5);

            bool ok = healer.Inventory.TryUseStaff(target, out _, out string fail);

            Assert.IsFalse(ok);
            Assert.IsNotNull(fail);
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
