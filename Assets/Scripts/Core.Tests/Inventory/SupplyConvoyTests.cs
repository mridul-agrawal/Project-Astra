using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class SupplyConvoyTests
    {
        private SupplyConvoy _convoy;

        [SetUp]
        public void SetUp()
        {
            _convoy = new SupplyConvoy();
        }

        [Test]
        public void Deposit_AddsItem()
        {
            Assert.IsTrue(_convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword)));
            Assert.AreEqual(1, _convoy.Count);
        }

        [Test]
        public void Deposit_EmptyItem_ReturnsFalse()
        {
            Assert.IsFalse(_convoy.TryDeposit(InventoryItem.None));
            Assert.AreEqual(0, _convoy.Count);
        }

        [Test]
        public void Deposit_SortsByWeaponType_SwordBeforeAxe()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponType.Sword, _convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Axe, _convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void Deposit_SortsByTierWithinType_IronBeforeSteel()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.SteelSword));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponTier.Iron, _convoy.GetSlot(0).weapon.tier);
            Assert.AreEqual(WeaponTier.Steel, _convoy.GetSlot(1).weapon.tier);
        }

        [Test]
        public void Deposit_StaffsAfterRegularWeapons()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.Heal));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(WeaponType.Sword, _convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Staff, _convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void Deposit_ConsumablesAfterWeapons()
        {
            _convoy.TryDeposit(InventoryItem.FromConsumable(ConsumableData.Vulnerary));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.AreEqual(ItemKind.Weapon, _convoy.GetSlot(0).kind);
            Assert.AreEqual(ItemKind.Consumable, _convoy.GetSlot(1).kind);
        }

        [Test]
        public void Deposit_WhenFull_ReturnsFalse()
        {
            for (int i = 0; i < SupplyConvoy.MaxCapacity; i++)
                _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));

            Assert.IsTrue(_convoy.IsFull);
            Assert.IsFalse(_convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe)));
            Assert.AreEqual(SupplyConvoy.MaxCapacity, _convoy.Count);
        }

        [Test]
        public void Withdraw_RemovesItem()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            Assert.IsTrue(_convoy.TryWithdraw(0, out var item));
            Assert.AreEqual("Loha Khadga", item.DisplayName);
            Assert.AreEqual(0, _convoy.Count);
        }

        [Test]
        public void Withdraw_OutOfRange_ReturnsFalse()
        {
            Assert.IsFalse(_convoy.TryWithdraw(0, out _));
            Assert.IsFalse(_convoy.TryWithdraw(-1, out _));
        }

        [Test]
        public void Withdraw_PreservesSortOrder()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronLance));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));

            _convoy.TryWithdraw(1, out _); // Remove Lance (middle)

            Assert.AreEqual(WeaponType.Sword, _convoy.GetSlot(0).weapon.weaponType);
            Assert.AreEqual(WeaponType.Axe, _convoy.GetSlot(1).weapon.weaponType);
        }

        [Test]
        public void IsAvailable_AlwaysTrue()
        {
            Assert.IsTrue(_convoy.IsAvailable);
        }

        [Test]
        public void GetSlot_OutOfRange_ReturnsNone()
        {
            Assert.IsTrue(_convoy.GetSlot(0).IsEmpty);
            Assert.IsTrue(_convoy.GetSlot(-1).IsEmpty);
            Assert.IsTrue(_convoy.GetSlot(999).IsEmpty);
        }

        [Test]
        public void ToArray_LoadFrom_RoundTrips()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronAxe));
            _convoy.TryDeposit(InventoryItem.FromConsumable(ConsumableData.Vulnerary));

            var snapshot = _convoy.ToArray();
            var restored = new SupplyConvoy();
            restored.LoadFrom(snapshot);

            Assert.AreEqual(_convoy.Count, restored.Count);
            for (int i = 0; i < _convoy.Count; i++)
                Assert.AreEqual(_convoy.GetSlot(i).DisplayName, restored.GetSlot(i).DisplayName);
        }

        [Test]
        public void OnConvoyChanged_FiresOnDeposit()
        {
            int count = 0;
            _convoy.OnConvoyChanged += () => count++;
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnConvoyChanged_FiresOnWithdraw()
        {
            _convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword));
            int count = 0;
            _convoy.OnConvoyChanged += () => count++;
            _convoy.TryWithdraw(0, out _);
            Assert.AreEqual(1, count);
        }
    }

    [TestFixture]
    public class ItemSortComparerTests
    {
        private ItemSortComparer _comparer;

        [SetUp]
        public void SetUp()
        {
            _comparer = new ItemSortComparer();
        }

        [Test]
        public void Weapons_SortByType_SwordBeforeLance()
        {
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            var lance = InventoryItem.FromWeapon(WeaponData.IronLance);
            Assert.Less(_comparer.Compare(sword, lance), 0);
        }

        [Test]
        public void SameType_SortByTier_IronBeforeSteel()
        {
            var iron = InventoryItem.FromWeapon(WeaponData.IronSword);
            var steel = InventoryItem.FromWeapon(WeaponData.SteelSword);
            Assert.Less(_comparer.Compare(iron, steel), 0);
        }

        [Test]
        public void Staff_SortsAfterRegularWeapons()
        {
            var staff = InventoryItem.FromWeapon(WeaponData.Heal);
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            Assert.Greater(_comparer.Compare(staff, sword), 0);
        }

        [Test]
        public void Consumables_SortAfterAllWeapons()
        {
            var vuln = InventoryItem.FromConsumable(ConsumableData.Vulnerary);
            var staff = InventoryItem.FromWeapon(WeaponData.Heal);
            Assert.Greater(_comparer.Compare(vuln, staff), 0);
        }

        [Test]
        public void EmptyItems_SortToEnd()
        {
            var empty = InventoryItem.None;
            var sword = InventoryItem.FromWeapon(WeaponData.IronSword);
            Assert.Greater(_comparer.Compare(empty, sword), 0);
        }
    }
}
