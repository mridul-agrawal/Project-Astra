using NUnit.Framework;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class SupplyConvoyTests
    {
        private SupplyConvoy convoy;

        [SetUp]
        public void SetUp()
        {
            convoy = new SupplyConvoy();
        }

        [Test]
        public void Deposit_AddsItem()
        {
            Assert.IsTrue(convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword)));
            Assert.AreEqual(1, convoy.Count);
        }

        [Test]
        public void Deposit_EmptyItem_ReturnsFalse()
        {
            Assert.IsFalse(convoy.TryDeposit(InventoryItem.None));
            Assert.AreEqual(0, convoy.Count);
        }

        [Test]
        public void Deposit_SortsByWeaponType_SwordBeforeAxe()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponType.Sword, convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Axe, convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void Deposit_SortsByTierWithinType_IronBeforeSteel()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.SteelSword));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponTier.Iron, convoy.GetSlot(0).weapon.tier);
            Assert.AreEqual(WeaponTier.Steel, convoy.GetSlot(1).weapon.tier);
        }

        [Test]
        public void Deposit_StaffsAfterRegularWeapons()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.Heal));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponType.Sword, convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Staff, convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void Deposit_ConsumablesAfterWeapons()
        {
            convoy.TryDeposit(InventoryItem.FromConsumable(ConsumableData.Vulnerary));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(ItemKind.Weapon, convoy.GetSlot(0).kind);
            Assert.AreEqual(ItemKind.Consumable, convoy.GetSlot(1).kind);
        }

        [Test]
        public void Deposit_WhenFull_ReturnsFalse()
        {
            for (int i = 0; i < SupplyConvoy.MaxCapacity; i++)
                convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.IsTrue(convoy.IsFull);
            Assert.IsFalse(convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe)));
            Assert.AreEqual(SupplyConvoy.MaxCapacity, convoy.Count);
        }

        [Test]
        public void Withdraw_RemovesItem()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            Assert.IsTrue(convoy.TryWithdraw(0, out var item));
            Assert.AreEqual("Loha Khadga", item.DisplayName);
            Assert.AreEqual(0, convoy.Count);
        }

        [Test]
        public void Withdraw_OutOfRange_ReturnsFalse()
        {
            Assert.IsFalse(convoy.TryWithdraw(0, out _));
            Assert.IsFalse(convoy.TryWithdraw(-1, out _));
        }

        [Test]
        public void Withdraw_PreservesSortOrder()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronLance));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));

            convoy.TryWithdraw(1, out _); // Remove Lance (middle)

            Assert.AreEqual(WeaponType.Sword, convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Axe, convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void IsAvailable_AlwaysTrue()
        {
            Assert.IsTrue(convoy.IsAvailable);
        }

        [Test]
        public void GetSlot_OutOfRange_ReturnsNone()
        {
            Assert.IsTrue(convoy.GetSlot(0).IsEmpty);
            Assert.IsTrue(convoy.GetSlot(-1).IsEmpty);
            Assert.IsTrue(convoy.GetSlot(999).IsEmpty);
        }

        [Test]
        public void ToArray_LoadFrom_RoundTrips()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));
            convoy.TryDeposit(InventoryItem.FromConsumable(ConsumableData.Vulnerary));

            var snapshot = convoy.ToArray();
            var restored = new SupplyConvoy();
            restored.LoadFrom(snapshot);

            Assert.AreEqual(convoy.Count, restored.Count);
            for (int i = 0; i < convoy.Count; i++)
                Assert.AreEqual(convoy.GetSlot(i).DisplayName, restored.GetSlot(i).DisplayName);
        }

        [Test]
        public void OnConvoyChanged_FiresOnDeposit()
        {
            int count = 0;
            convoy.OnConvoyChanged += () => count++;
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnConvoyChanged_FiresOnWithdraw()
        {
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            int count = 0;
            convoy.OnConvoyChanged += () => count++;
            convoy.TryWithdraw(0, out _);
            Assert.AreEqual(1, count);
        }
    }

    [TestFixture]
    public class ItemSortComparerTests
    {
        private ItemSortComparer comparer;

        [SetUp]
        public void SetUp()
        {
            comparer = new ItemSortComparer();
        }

        [Test]
        public void Weapons_SortByType_SwordBeforeLance()
        {
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            var lance = InventoryItem.FromWeapon(WeaponData.IronLance);
            Assert.Less(comparer.Compare(sword, lance), 0);
        }

        [Test]
        public void SameType_SortByTier_IronBeforeSteel()
        {
            var iron = InventoryItem.FromWeapon(WeaponData.IronSword);
            var steel = InventoryItem.FromWeapon(WeaponData.SteelSword);
            Assert.Less(comparer.Compare(iron, steel), 0);
        }

        [Test]
        public void Staff_SortsAfterRegularWeapons()
        {
            var staff = InventoryItem.FromWeapon(WeaponData.Heal);
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            Assert.Greater(comparer.Compare(staff, sword), 0);
        }

        [Test]
        public void Consumables_SortAfterAllWeapons()
        {
            var vuln = InventoryItem.FromConsumable(ConsumableData.Vulnerary);
            var staff = InventoryItem.FromWeapon(WeaponData.Heal);
            Assert.Greater(comparer.Compare(vuln, staff), 0);
        }

        [Test]
        public void EmptyItems_SortToEnd()
        {
            var empty = InventoryItem.None;
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            Assert.Greater(comparer.Compare(empty, sword), 0);
        }
    }
}
