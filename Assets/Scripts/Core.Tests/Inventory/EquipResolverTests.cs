using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class EquipResolverTests
    {
        private TestUnit unit;

        [SetUp]
        public void SetUp()
        {
            unit = new GameObject("EquipResolverUnit").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            if (unit != null) Object.DestroyImmediate(unit.gameObject);
        }

        [Test]
        public void EmptyWeapon_NotEquippable()
        {
            Assert.IsFalse(EquipResolver.CanEquip(unit, WeaponData.None));
        }

        [Test]
        public void BrokenWeapon_NotEquippable()
        {
            var sword = TestItems.IronSword;
            sword.currentUses = 0;
            Assert.IsFalse(EquipResolver.CanEquip(unit, sword));
        }

        [Test]
        public void NoConstraints_AllowsAnything()
        {
            // No tracker, no class, no allowedWeaponTypes → permissive.
            Assert.IsTrue(EquipResolver.CanEquip(unit, TestItems.IronSword));
        }

        [Test]
        public void RankTracker_Defers_AllowsRankMet()
        {
            unit.WeaponRankTracker = new WeaponRankTracker();
            unit.WeaponRankTracker.InitializeRank(WeaponType.Sword, WeaponRank.B);
            Assert.IsTrue(EquipResolver.CanEquip(unit, TestItems.SilverSword));
        }

        [Test]
        public void RankTracker_Defers_RejectsRankTooLow()
        {
            unit.WeaponRankTracker = new WeaponRankTracker();
            unit.WeaponRankTracker.InitializeRank(WeaponType.Sword, WeaponRank.E);
            Assert.IsFalse(EquipResolver.CanEquip(unit, TestItems.SilverSword));
        }

        [Test]
        public void RankTracker_Defers_RejectsNoAccess()
        {
            unit.WeaponRankTracker = new WeaponRankTracker();
            unit.WeaponRankTracker.InitializeRank(WeaponType.Lance, WeaponRank.A);
            Assert.IsFalse(EquipResolver.CanEquip(unit, TestItems.IronSword));
        }

        [Test]
        public void AllowedWeaponTypesFallback_Allows()
        {
            SetAllowedTypes(unit, WeaponType.Sword);
            Assert.IsTrue(EquipResolver.CanEquip(unit, TestItems.IronSword));
        }

        [Test]
        public void AllowedWeaponTypesFallback_Rejects()
        {
            SetAllowedTypes(unit, WeaponType.AnimaTome);
            Assert.IsFalse(EquipResolver.CanEquip(unit, TestItems.IronSword));
        }

        [Test]
        public void CharacterLockedWeapon_NonOwner_Rejected()
        {
            var locked = TestItems.IronSword;
            locked.characterLocked = true;
            locked.ownerUnitId = "krishna";

            // No UnitInstance → unitId resolves to null → mismatch with "krishna".
            Assert.IsFalse(EquipResolver.CanEquip(unit, locked));
        }

        private static void SetAllowedTypes(TestUnit unit, params WeaponType[] types)
        {
            var field = typeof(TestUnit).GetField("allowedWeaponTypes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(unit, types);
        }
    }
}
