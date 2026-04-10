using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class TradeSessionTests
    {
        private TestUnit _left;
        private TestUnit _right;

        [SetUp]
        public void SetUp()
        {
            _left = new GameObject("LeftUnit").AddComponent<TestUnit>();
            _right = new GameObject("RightUnit").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_left != null) Object.DestroyImmediate(_left.gameObject);
            if (_right != null) Object.DestroyImmediate(_right.gameObject);
        }

        [Test]
        public void Constructor_CopiesSlotsFromBothUnits()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            var session = new TradeSession(_left, _right);

            Assert.AreEqual("Loha Khadga", session.GetLeftSlot(0).DisplayName);
            Assert.AreEqual("Loha Parashu", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TrySwap_ExchangesItems()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            bool ok = session.TrySwap(0, 0);

            Assert.IsTrue(ok);
            Assert.AreEqual("Loha Parashu", session.GetLeftSlot(0).DisplayName);
            Assert.AreEqual("Loha Khadga", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TrySwap_FailsWhenEitherSlotEmpty()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(_left, _right);

            Assert.IsFalse(session.TrySwap(0, 0));
        }

        [Test]
        public void TryGive_MovesItemLeftToRight()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(_left, _right);

            bool ok = session.TryGive(0);

            Assert.IsTrue(ok);
            Assert.IsTrue(session.GetLeftSlot(0).IsEmpty);
            Assert.AreEqual("Loha Khadga", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TryGive_FailsWhenRightIsFull()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            for (int i = 0; i < UnitInventory.Capacity; i++)
                _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            Assert.IsFalse(session.TryGive(0));
        }

        [Test]
        public void TryTake_MovesItemRightToLeft()
        {
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            bool ok = session.TryTake(0);

            Assert.IsTrue(ok);
            Assert.IsTrue(session.GetRightSlot(0).IsEmpty);
            Assert.AreEqual("Loha Parashu", session.GetLeftSlot(0).DisplayName);
        }

        [Test]
        public void TryTake_FailsWhenLeftIsFull()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            Assert.IsFalse(session.TryTake(0));
        }

        [Test]
        public void Commit_WritesBackToRealInventories()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            session.TrySwap(0, 0);
            session.Commit();

            Assert.AreEqual("Loha Parashu", _left.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Loha Khadga", _right.Inventory.GetSlot(0).DisplayName);
        }

        [Test]
        public void DiscardingSession_LeavesInventoriesUnchanged()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            session.TrySwap(0, 0);
            // Intentionally NOT calling session.Commit()

            Assert.AreEqual("Loha Khadga", _left.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Loha Parashu", _right.Inventory.GetSlot(0).DisplayName);
        }

        [Test]
        public void MultipleOperations_AllApplyOnCommit()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _left.Inventory.TryAddItem(InventoryItem.FromConsumable(ConsumableData.Vulnerary), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            session.TryGive(1); // Give Vulnerary to right
            session.TrySwap(0, 0); // Swap swords
            session.Commit();

            Assert.AreEqual("Loha Parashu", _left.Inventory.GetSlot(0).DisplayName);
            Assert.IsTrue(_left.Inventory.GetSlot(1).IsEmpty);
            Assert.AreEqual("Loha Khadga", _right.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Sanjivani", _right.Inventory.GetSlot(1).DisplayName);
        }

        [Test]
        public void BothFull_OnlySwapWorks()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
                _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            }
            var session = new TradeSession(_left, _right);

            Assert.IsFalse(session.CanGive(0));
            Assert.IsFalse(session.CanTake(0));
            Assert.IsTrue(session.CanSwap(0, 0));
            Assert.IsTrue(session.TrySwap(0, 0));
        }

        [Test]
        public void HasChanges_FalseInitially_TrueAfterOperation()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            _right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(_left, _right);

            Assert.IsFalse(session.HasChanges);
            session.TrySwap(0, 0);
            Assert.IsTrue(session.HasChanges);
        }

        [Test]
        public void EquippedWeapon_ReResolvesAfterCommit()
        {
            _left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(_left, _right);

            session.TryGive(0); // Give away the only weapon
            session.Commit();

            Assert.IsTrue(_left.Inventory.IsUnarmed);
            Assert.IsFalse(_right.Inventory.IsUnarmed);
        }
    }
}
