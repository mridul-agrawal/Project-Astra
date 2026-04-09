using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class EquipResolverTests
    {
        private TestUnit _unit;

        [SetUp]
        public void SetUp()
        {
            _unit = new GameObject("EquipResolverUnit").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_unit != null) Object.DestroyImmediate(_unit.gameObject);
        }

        [Test]
        public void EmptyWeapon_NotEquippable()
        {
            Assert.IsFalse(EquipResolver.CanEquip(_unit, WeaponData.None));
        }

        [Test]
        public void BrokenWeapon_NotEquippable()
        {
            var sword = WeaponData.IronSword;
            sword.currentUses = 0;
            Assert.IsFalse(EquipResolver.CanEquip(_unit, sword));
        }

        [Test]
        public void NoConstraints_AllowsAnything()
        {
            // No tracker, no class, no allowedWeaponTypes → permissive.
            Assert.IsTrue(EquipResolver.CanEquip(_unit, WeaponData.IronSword));
        }

        [Test]
        public void RankTracker_Defers_AllowsRankMet()
        {
            _unit.WeaponRankTracker = new WeaponRankTracker();
            _unit.WeaponRankTracker.InitializeRank(WeaponType.Sword, WeaponRank.B);
            Assert.IsTrue(EquipResolver.CanEquip(_unit, WeaponData.SilverSword));
        }

        [Test]
        public void RankTracker_Defers_RejectsRankTooLow()
        {
            _unit.WeaponRankTracker = new WeaponRankTracker();
            _unit.WeaponRankTracker.InitializeRank(WeaponType.Sword, WeaponRank.E);
            Assert.IsFalse(EquipResolver.CanEquip(_unit, WeaponData.SilverSword));
        }

        [Test]
        public void RankTracker_Defers_RejectsNoAccess()
        {
            _unit.WeaponRankTracker = new WeaponRankTracker();
            _unit.WeaponRankTracker.InitializeRank(WeaponType.Lance, WeaponRank.A);
            Assert.IsFalse(EquipResolver.CanEquip(_unit, WeaponData.IronSword));
        }

        [Test]
        public void AllowedWeaponTypesFallback_Allows()
        {
            SetAllowedTypes(_unit, WeaponType.Sword);
            Assert.IsTrue(EquipResolver.CanEquip(_unit, WeaponData.IronSword));
        }

        [Test]
        public void AllowedWeaponTypesFallback_Rejects()
        {
            SetAllowedTypes(_unit, WeaponType.AnimaTome);
            Assert.IsFalse(EquipResolver.CanEquip(_unit, WeaponData.IronSword));
        }

        [Test]
        public void CharacterLockedWeapon_NonOwner_Rejected()
        {
            var locked = WeaponData.IronSword;
            locked.characterLocked = true;
            locked.ownerUnitId = "krishna";

            // No UnitInstance → unitId resolves to null → mismatch with "krishna".
            Assert.IsFalse(EquipResolver.CanEquip(_unit, locked));
        }

        private static void SetAllowedTypes(TestUnit unit, params WeaponType[] types)
        {
            var field = typeof(TestUnit).GetField("_allowedWeaponTypes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(unit, types);
        }
    }
}
