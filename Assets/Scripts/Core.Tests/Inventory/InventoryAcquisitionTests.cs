using System;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Inventory
{
    [TestFixture]
    public class InventoryAcquisitionTests
    {
        private TestUnit unit;
        private IInventoryFullPromptHandler previousHandler;
        private IConvoy previousConvoy;

        [SetUp]
        public void SetUp()
        {
            unit = new GameObject("AcquisitionTestUnit").AddComponent<TestUnit>();
            previousHandler = InventoryAcquisition.PromptHandler;
            previousConvoy = Convoy.Current;
            // Default to no-convoy so each test exercises the prompt path unless it opts in.
            Convoy.Current = NullConvoy.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            InventoryAcquisition.PromptHandler = previousHandler;
            Convoy.Current = previousConvoy;
            if (unit != null) UnityEngine.Object.DestroyImmediate(unit.gameObject);
        }

        [Test]
        public void EmptyInventory_AddsImmediately()
        {
            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.IronSword),
                r => captured = r);

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(AcquisitionOutcome.Added, captured.Value.Outcome);
            Assert.AreEqual(0, captured.Value.SlotIndex);
            Assert.AreEqual(1, unit.Inventory.OccupiedCount);
        }

        [Test]
        public void FullInventory_NoHandler_ReturnsCanceled()
        {
            FillInventory();
            InventoryAcquisition.PromptHandler = null;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Canceled, captured.Value.Outcome);
            Assert.AreEqual(5, unit.Inventory.OccupiedCount);
        }

        [Test]
        public void FullInventory_HandlerDiscards_Swaps()
        {
            FillInventory();
            var handler = new TestPromptHandler { ChooseDiscardSlot = 2 };
            InventoryAcquisition.PromptHandler = handler;

            var incoming = InventoryItem.FromWeapon(TestItems.SteelSword);
            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(unit, incoming, r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Swapped, captured.Value.Outcome);
            Assert.AreEqual(2, captured.Value.SlotIndex);
            Assert.AreEqual(WeaponTier.Steel, unit.Inventory.GetSlot(2).weapon.tier);
        }

        [Test]
        public void FullInventory_HandlerCancels_StateUnchanged()
        {
            FillInventory();
            var snapshot = unit.Inventory.GetSlot(0).weapon.name;
            var handler = new TestPromptHandler { ShouldCancel = true };
            InventoryAcquisition.PromptHandler = handler;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Canceled, captured.Value.Outcome);
            Assert.AreEqual(snapshot, unit.Inventory.GetSlot(0).weapon.name);
        }

        [Test]
        public void NullConvoy_IsUnavailable()
        {
            Assert.IsFalse(NullConvoy.Instance.IsAvailable);
            Assert.IsFalse(NullConvoy.Instance.TryDeposit(InventoryItem.FromWeapon(TestItems.IronSword)));
        }

        [Test]
        public void FullInventory_ConvoyAvailable_SendsToConvoy()
        {
            FillInventory();
            var convoy = new SupplyConvoy();
            Convoy.Current = convoy;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.SentToConvoy, captured.Value.Outcome);
            Assert.AreEqual(1, convoy.Count);
        }

        [Test]
        public void FullInventory_ConvoyAlsoFull_FallsThrough()
        {
            FillInventory();
            var convoy = new SupplyConvoy();
            for (int i = 0; i < SupplyConvoy.MaxCapacity; i++)
                convoy.TryDeposit(InventoryItem.FromWeapon(TestItems.IronAxe));
            Convoy.Current = convoy;
            InventoryAcquisition.PromptHandler = null;

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.Canceled, captured.Value.Outcome);
        }

        [Test]
        public void FullInventory_ConvoyTakesPriority_OverPromptHandler()
        {
            FillInventory();
            var convoy = new SupplyConvoy();
            Convoy.Current = convoy;
            bool handlerCalled = false;
            InventoryAcquisition.PromptHandler = new TestPromptHandler { ShouldCancel = true };

            AcquisitionResult? captured = null;
            InventoryAcquisition.TryAcquireItem(
                unit,
                InventoryItem.FromWeapon(TestItems.SteelSword),
                r => captured = r);

            Assert.AreEqual(AcquisitionOutcome.SentToConvoy, captured.Value.Outcome);
            Assert.IsFalse(handlerCalled);
        }

        private void FillInventory()
        {
            for (int i = 0; i < UnitInventory.Capacity; i++)
                unit.Inventory.TryAddItem(InventoryItem.FromWeapon(TestItems.IronSword), out _);
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
