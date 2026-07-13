using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class UnitInventoryTests
    {
        private TestUnit unit;
        private UnitInventory inventory;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("InventoryTestUnit");
            unit = go.AddComponent<TestUnit>();
            inventory = unit.Inventory;
        }

        [TearDown]
        public void TearDown()
        {
            if (unit != null) Object.DestroyImmediate(unit.gameObject);
        }

        [Test]
        public void Capacity_IsFive()
        {
            Assert.AreEqual(5, UnitInventory.Capacity);
        }

        [Test]
        public void NewInventory_IsEmpty()
        {
            Assert.AreEqual(0, inventory.OccupiedCount);
            Assert.IsTrue(inventory.IsEmpty);
            Assert.IsFalse(inventory.IsFull);
        }

        [Test]
        public void TryAddItem_AppendsToFirstEmptySlot()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out int slot);
            Assert.AreEqual(0, slot);

            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out int slot2);
            Assert.AreEqual(1, slot2);
            Assert.AreEqual(2, inventory.OccupiedCount);
        }

        [Test]
        public void TryAddItem_FailsWhenFull()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);

            Assert.IsTrue(inventory.IsFull);
            bool added = inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out int slot);
            Assert.IsFalse(added);
            Assert.AreEqual(-1, slot);
        }

        [Test]
        public void DiscardSlot_ClearsAndFiresChanged()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            int changedCount = 0;
            inventory.OnInventoryChanged += () => changedCount++;

            inventory.DiscardSlot(0);
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void SwapSlots_PermutesCorrectly()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out _);

            inventory.SwapSlots(0, 1);

            Assert.AreEqual(WeaponType.Axe, inventory.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Sword, inventory.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void GetEquippedWeapon_ScansFromSlot0()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out _);

            Assert.AreEqual(0, inventory.EquippedWeaponSlot);
            Assert.AreEqual("Loha Khadga", inventory.GetEquippedWeapon().name);
        }

        [Test]
        public void GetEquippedWeapon_SkipsConsumablesInSlot0()
        {
            inventory.TryAddItem(InventoryItem.FromConsumable(TestItems.Vulnerary), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);

            Assert.AreEqual(1, inventory.EquippedWeaponSlot);
            Assert.IsFalse(inventory.IsUnarmed);
        }

        [Test]
        public void GetEquippedWeapon_SkipsBrokenWeapons()
        {
            var broken = TestItems.IronSword;
            broken.currentUses = 0;
            var fresh = TestItems.IronAxe;

            inventory.TryAddItem(InventoryItem.FromWeapon(broken), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(fresh), out _);

            Assert.AreEqual(1, inventory.EquippedWeaponSlot);
        }

        [Test]
        public void MageWith5Swords_IsUnarmed_DoesNotCrash()
        {
            // Force class whitelist to AnimaTome only via an allowedWeaponTypes fallback.
            // We rely on the [SerializeField] private _allowedWeaponTypes — set via
            // a temp wrapper since unity SerializedProperty isn't available in editmode tests.
            // Easier path: use reflection to set the private field.
            var field = typeof(TestUnit).GetField("allowedWeaponTypes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(unit, new[] { WeaponType.AnimaTome });

            for (int i = 0; i < 5; i++)
                inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);

            Assert.IsTrue(inventory.IsUnarmed);
            Assert.IsTrue(inventory.GetEquippedWeapon().IsEmpty);
        }

        [Test]
        public void DiscardingEquippedWeapon_NextWeaponBecomesEquipped()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out _);

            inventory.DiscardSlot(0);

            Assert.AreEqual(WeaponType.Axe, inventory.GetEquippedWeapon().weaponType);
        }

        [Test]
        public void DiscardingOnlyWeapon_BecomesUnarmed()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.DiscardSlot(0);

            Assert.IsTrue(inventory.IsUnarmed);
        }

        [Test]
        public void ConsumeEquippedWeaponUses_DecrementsAndClearsOnBreak()
        {
            var sword = TestItems.IronSword;
            sword.currentUses = 1;
            inventory.TryAddItem(InventoryItem.FromWeapon(sword), out _);

            int destroyedCount = 0;
            inventory.OnItemDestroyed += _ => destroyedCount++;

            inventory.ConsumeEquippedWeaponUses(1);

            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(1, destroyedCount);
            Assert.IsTrue(inventory.IsUnarmed);
        }

        [Test]
        public void ConsumeEquippedWeaponUses_DecrementsWithoutBreaking()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.ConsumeEquippedWeaponUses(1);
            Assert.AreEqual(44, inventory.GetSlot(0).weapon.currentUses);
        }

        [Test]
        public void IndestructibleWeapon_NeverDecrements()
        {
            var sword = TestItems.IronSword;
            sword.indestructible = true;
            inventory.TryAddItem(InventoryItem.FromWeapon(sword), out _);

            inventory.ConsumeEquippedWeaponUses(10);

            Assert.AreEqual(45, inventory.GetSlot(0).weapon.currentUses);
            Assert.IsFalse(inventory.IsUnarmed);
        }

        [Test]
        public void EquipFromSlot_MovesWeaponToSlotZero()
        {
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
            inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronAxe), out _);

            inventory.EquipFromSlot(1);

            Assert.AreEqual(WeaponType.Axe, inventory.GetEquippedWeapon().weaponType);
            Assert.AreEqual(0, inventory.EquippedWeaponSlot);
        }

        [Test]
        public void TryUseConsumable_AppliesEffectAndDecrements()
        {
            unit.maxHP = 30;
            unit.currentHP = 10;
            inventory.TryAddItem(InventoryItem.FromConsumable(TestItems.Vulnerary), out _);

            bool ok = inventory.TryUseConsumable(0, out string fail);

            Assert.IsTrue(ok, fail);
            Assert.AreEqual(20, unit.currentHP);
            Assert.AreEqual(2, inventory.GetSlot(0).consumable.currentUses);
        }

        [Test]
        public void TryUseConsumable_DepletedItem_ClearsSlot()
        {
            unit.maxHP = 30;
            unit.currentHP = 10;
            var v = TestItems.Vulnerary;
            v.currentUses = 1;
            inventory.TryAddItem(InventoryItem.FromConsumable(v), out _);

            inventory.TryUseConsumable(0, out _);

            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
        }
    }
}
