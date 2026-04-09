using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class UnitInventoryTests
    {
        private TestUnit _unit;
        private UnitInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("InventoryTestUnit");
            _unit = go.AddComponent<TestUnit>();
            _inventory = _unit.Inventory;
        }

        [TearDown]
        public void TearDown()
        {
            if (_unit != null) Object.DestroyImmediate(_unit.gameObject);
        }

        [Test]
        public void Capacity_IsFive()
        {
            Assert.AreEqual(5, UnitInventory.Capacity);
        }

        [Test]
        public void NewInventory_IsEmpty()
        {
            Assert.AreEqual(0, _inventory.OccupiedCount);
            Assert.IsTrue(_inventory.IsEmpty);
            Assert.IsFalse(_inventory.IsFull);
        }

        [Test]
        public void TryAddItem_AppendsToFirstEmptySlot()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out int slot);
            Assert.AreEqual(0, slot);

            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out int slot2);
            Assert.AreEqual(1, slot2);
            Assert.AreEqual(2, _inventory.OccupiedCount);
        }

        [Test]
        public void TryAddItem_FailsWhenFull()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);

            Assert.IsTrue(_inventory.IsFull);
            bool added = _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out int slot);
            Assert.IsFalse(added);
            Assert.AreEqual(-1, slot);
        }

        [Test]
        public void DiscardSlot_ClearsAndFiresChanged()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            int changedCount = 0;
            _inventory.OnInventoryChanged += () => changedCount++;

            _inventory.DiscardSlot(0);
            Assert.IsTrue(_inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void SwapSlots_PermutesCorrectly()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            _inventory.SwapSlots(0, 1);

            Assert.AreEqual(WeaponType.Axe, _inventory.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Sword, _inventory.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void GetEquippedWeapon_ScansFromSlot0()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            Assert.AreEqual(0, _inventory.EquippedWeaponSlot);
            Assert.AreEqual("Loha Khadga", _inventory.GetEquippedWeapon().name);
        }

        [Test]
        public void GetEquippedWeapon_SkipsConsumablesInSlot0()
        {
            _inventory.TryAddItem(InventoryItem.FromConsumable(ConsumableData.Vulnerary), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);

            Assert.AreEqual(1, _inventory.EquippedWeaponSlot);
            Assert.IsFalse(_inventory.IsUnarmed);
        }

        [Test]
        public void GetEquippedWeapon_SkipsBrokenWeapons()
        {
            var broken = WeaponData.IronSword;
            broken.currentUses = 0;
            var fresh = WeaponData.IronAxe;

            _inventory.TryAddItem(InventoryItem.FromWeapon(broken), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(fresh), out _);

            Assert.AreEqual(1, _inventory.EquippedWeaponSlot);
        }

        [Test]
        public void MageWith5Swords_IsUnarmed_DoesNotCrash()
        {
            // Force class whitelist to AnimaTome only via an allowedWeaponTypes fallback.
            // We rely on the [SerializeField] private _allowedWeaponTypes — set via
            // a temp wrapper since unity SerializedProperty isn't available in editmode tests.
            // Easier path: use reflection to set the private field.
            var field = typeof(TestUnit).GetField("_allowedWeaponTypes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(_unit, new[] { WeaponType.AnimaTome });

            for (int i = 0; i < 5; i++)
                _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);

            Assert.IsTrue(_inventory.IsUnarmed);
            Assert.IsTrue(_inventory.GetEquippedWeapon().IsEmpty);
        }

        [Test]
        public void DiscardingEquippedWeapon_NextWeaponBecomesEquipped()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            _inventory.DiscardSlot(0);

            Assert.AreEqual(WeaponType.Axe, _inventory.GetEquippedWeapon().weaponType);
        }

        [Test]
        public void DiscardingOnlyWeapon_BecomesUnarmed()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.DiscardSlot(0);

            Assert.IsTrue(_inventory.IsUnarmed);
        }

        [Test]
        public void ConsumeEquippedWeaponUses_DecrementsAndClearsOnBreak()
        {
            var sword = WeaponData.IronSword;
            sword.currentUses = 1;
            _inventory.TryAddItem(InventoryItem.FromWeapon(sword), out _);

            int destroyedCount = 0;
            _inventory.OnItemDestroyed += _ => destroyedCount++;

            _inventory.ConsumeEquippedWeaponUses(1);

            Assert.IsTrue(_inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(1, destroyedCount);
            Assert.IsTrue(_inventory.IsUnarmed);
        }

        [Test]
        public void ConsumeEquippedWeaponUses_DecrementsWithoutBreaking()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.ConsumeEquippedWeaponUses(1);
            Assert.AreEqual(44, _inventory.GetSlot(0).weapon.currentUses);
        }

        [Test]
        public void IndestructibleWeapon_NeverDecrements()
        {
            var sword = WeaponData.IronSword;
            sword.indestructible = true;
            _inventory.TryAddItem(InventoryItem.FromWeapon(sword), out _);

            _inventory.ConsumeEquippedWeaponUses(10);

            Assert.AreEqual(45, _inventory.GetSlot(0).weapon.currentUses);
            Assert.IsFalse(_inventory.IsUnarmed);
        }

        [Test]
        public void EquipFromSlot_MovesWeaponToSlotZero()
        {
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            _inventory.EquipFromSlot(1);

            Assert.AreEqual(WeaponType.Axe, _inventory.GetEquippedWeapon().weaponType);
            Assert.AreEqual(0, _inventory.EquippedWeaponSlot);
        }

        [Test]
        public void TryUseConsumable_AppliesEffectAndDecrements()
        {
            _unit.maxHP = 30;
            _unit.currentHP = 10;
            _inventory.TryAddItem(InventoryItem.FromConsumable(ConsumableData.Vulnerary), out _);

            bool ok = _inventory.TryUseConsumable(0, out string fail);

            Assert.IsTrue(ok, fail);
            Assert.AreEqual(20, _unit.currentHP);
            Assert.AreEqual(2, _inventory.GetSlot(0).consumable.currentUses);
        }

        [Test]
        public void TryUseConsumable_DepletedItem_ClearsSlot()
        {
            _unit.maxHP = 30;
            _unit.currentHP = 10;
            var v = ConsumableData.Vulnerary;
            v.currentUses = 1;
            _inventory.TryAddItem(InventoryItem.FromConsumable(v), out _);

            _inventory.TryUseConsumable(0, out _);

            Assert.IsTrue(_inventory.GetSlot(0).IsEmpty);
        }
    }
}
