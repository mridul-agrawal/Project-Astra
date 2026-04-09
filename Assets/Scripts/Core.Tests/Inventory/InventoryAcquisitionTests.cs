using System;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class InventoryAcquisitionTests
    {
        private TestUnit _unit;
        private IInventoryFullPromptHandler _previousHandler;

        [SetUp]
        public void SetUp()
        {
            _unit = new GameObject("AcquisitionTestUnit").AddComponent<TestUnit>();
            _previousHandler = InventoryAcquisition.PromptHandler;
        }

        [TearDown]
        public void TearDown()
        {
            InventoryAcquisition.PromptHandler = _previousHandler;
            if (_unit != null) UnityEngine.Object.DestroyImmediate(_unit.gameObject);
        }

        [Test]
        public void EmptyInventory_AddsImmediately()
        {
            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                _unit,
                InventoryItem.FromWeapon(WeaponData.IronSword),
                r => captured = r);

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(AcquisitionOutcome.Added, captured.Value.Outcome);
            Assert.AreEqual(0, captured.Value.SlotIndex);
            Assert.AreEqual(1, _unit.Inventory.OccupiedCount);
        }

        [Test]
        public void FullInventory_NoHandler_ReturnsCanceled()
        {
            FillInventory();
            InventoryAcquisition.PromptHandler = null;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                _unit,
                InventoryItem.FromWeapon(WeaponData.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Canceled, captured.Value.Outcome);
            Assert.AreEqual(5, _unit.Inventory.OccupiedCount);
        }

        [Test]
        public void FullInventory_HandlerDiscards_Swaps()
        {
            FillInventory();
            var handler = new TestPromptHandler { ChooseDiscardSlot = 2 };
            InventoryAcquisition.PromptHandler = handler;

            var incoming = InventoryItem.FromWeapon(WeaponData.SteelSword);
            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(_unit, incoming, r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Swapped, captured.Value.Outcome);
            Assert.AreEqual(2, captured.Value.SlotIndex);
            Assert.AreEqual(WeaponTier.Steel, _unit.Inventory.GetSlot(2).weapon.tier);
        }

        [Test]
        public void FullInventory_HandlerCancels_StateUnchanged()
        {
            FillInventory();
            var snapshot = _unit.Inventory.GetSlot(0).weapon.name;
            var handler = new TestPromptHandler { ShouldCancel = true };
            InventoryAcquisition.PromptHandler = handler;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                _unit,
                InventoryItem.FromWeapon(WeaponData.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Canceled, captured.Value.Outcome);
            Assert.AreEqual(snapshot, _unit.Inventory.GetSlot(0).weapon.name);
        }

        [Test]
        public void NullConvoy_IsUnavailable()
        {
            Assert.IsFalse(NullConvoy.Instance.IsAvailable);
            Assert.IsFalse(NullConvoy.Instance.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronSword)));
        }

        private void FillInventory()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                _unit.Inventory.TryAddItem(InventoryItem.FromWeapon(WeaponData.IronSword), out _);
        }

        private class TestPromptHandler : IInventoryFullPromptHandler
        {
            public int ChooseDiscardSlot = 0;
            public bool ShouldCancel = false;

            public void Prompt(TestUnit unit, InventoryItem incoming,
                Action<int> onChooseDiscardSlot, Action onCancel)
            {
                if (ShouldCancel) onCancel?.Invoke();
                else onChooseDiscardSlot?.Invoke(ChooseDiscardSlot);
            }
        }
    }
}
