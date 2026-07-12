using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class TradeSessionTests
    {
        private TestUnit left;
        private TestUnit right;

        [SetUp]
        public void SetUp()
        {
            left = new GameObject("LeftUnit").AddComponent<TestUnit>();
            right = new GameObject("RightUnit").AddComponent<TestUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            if (left != null) Object.DestroyImmediate(left.gameObject);
            if (right != null) Object.DestroyImmediate(right.gameObject);
        }

        [Test]
        public void Constructor_CopiesSlotsFromBothUnits()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);

            var session = new TradeSession(left, right);

            Assert.AreEqual("Loha Khadga", session.GetLeftSlot(0).DisplayName);
            Assert.AreEqual("Loha Parashu", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TrySwap_ExchangesItems()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            bool ok = session.TrySwap(0, 0);

            Assert.IsTrue(ok);
            Assert.AreEqual("Loha Parashu", session.GetLeftSlot(0).DisplayName);
            Assert.AreEqual("Loha Khadga", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TrySwap_FailsWhenEitherSlotEmpty()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(left, right);

            Assert.IsFalse(session.TrySwap(0, 0));
        }

        [Test]
        public void TryGive_MovesItemLeftToRight()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(left, right);

            bool ok = session.TryGive(0);

            Assert.IsTrue(ok);
            Assert.IsTrue(session.GetLeftSlot(0).IsEmpty);
            Assert.AreEqual("Loha Khadga", session.GetRightSlot(0).DisplayName);
        }

        [Test]
        public void TryGive_FailsWhenRightIsFull()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            for (int i = 0; i < UnitInventory.Capacity; i++)
                right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            Assert.IsFalse(session.TryGive(0));
        }

        [Test]
        public void TryTake_MovesItemRightToLeft()
        {
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            bool ok = session.TryTake(0);

            Assert.IsTrue(ok);
            Assert.IsTrue(session.GetRightSlot(0).IsEmpty);
            Assert.AreEqual("Loha Parashu", session.GetLeftSlot(0).DisplayName);
        }

        [Test]
        public void TryTake_FailsWhenLeftIsFull()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            Assert.IsFalse(session.TryTake(0));
        }

        [Test]
        public void Commit_WritesBackToRealInventories()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            session.TrySwap(0, 0);
            session.Commit();

            Assert.AreEqual("Loha Parashu", left.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Loha Khadga", right.Inventory.GetSlot(0).DisplayName);
        }

        [Test]
        public void DiscardingSession_LeavesInventoriesUnchanged()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            session.TrySwap(0, 0);
            // Intentionally NOT calling session.Commit()

            Assert.AreEqual("Loha Khadga", left.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Loha Parashu", right.Inventory.GetSlot(0).DisplayName);
        }

        [Test]
        public void MultipleOperations_AllApplyOnCommit()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            left.Inventory.TryAddItem(InventoryItem.FromConsumable(ConsumableData.Vulnerary), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            session.TryGive(1); // Give Vulnerary to right
            session.TrySwap(0, 0); // Swap swords
            session.Commit();

            Assert.AreEqual("Loha Parashu", left.Inventory.GetSlot(0).DisplayName);
            Assert.IsTrue(left.Inventory.GetSlot(1).IsEmpty);
            Assert.AreEqual("Loha Khadga", right.Inventory.GetSlot(0).DisplayName);
            Assert.AreEqual("Sanjivani", right.Inventory.GetSlot(1).DisplayName);
        }

        [Test]
        public void BothFull_OnlySwapWorks()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
                right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            }
            var session = new TradeSession(left, right);

            Assert.IsFalse(session.CanGive(0));
            Assert.IsFalse(session.CanTake(0));
            Assert.IsTrue(session.CanSwap(0, 0));
            Assert.IsTrue(session.TrySwap(0, 0));
        }

        [Test]
        public void HasChanges_FalseInitially_TrueAfterOperation()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            right.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronAxe), out _);
            var session = new TradeSession(left, right);

            Assert.IsFalse(session.HasChanges);
            session.TrySwap(0, 0);
            Assert.IsTrue(session.HasChanges);
        }

        [Test]
        public void EquippedWeapon_ReResolvesAfterCommit()
        {
            left.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
            var session = new TradeSession(left, right);

            session.TryGive(0); // Give away the only weapon
            session.Commit();

            Assert.IsTrue(left.Inventory.IsUnarmed);
            Assert.IsFalse(right.Inventory.IsUnarmed);
        }
    }
}
