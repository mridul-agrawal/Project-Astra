using System;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core
{
    public enum AcquisitionOutcome
    {
        Added,
        Swapped,
        SentToConvoy,
        Canceled,
    }

    public readonly struct AcquisitionResult
    {
        public readonly AcquisitionOutcome Outcome;
        public readonly int SlotIndex;
        public readonly InventoryItem DisplacedItem;

        public AcquisitionResult(AcquisitionOutcome outcome, int slotIndex, InventoryItem displaced)
        {
            Outcome = outcome;
            SlotIndex = slotIndex;
            DisplacedItem = displaced;
        }

        public static AcquisitionResult Added(int slot) =>
            new(AcquisitionOutcome.Added, slot, InventoryItem.None);

        public static AcquisitionResult Swapped(int slot, InventoryItem displaced) =>
            new(AcquisitionOutcome.Swapped, slot, displaced);

        public static AcquisitionResult SentToConvoy() =>
            new(AcquisitionOutcome.SentToConvoy, -1, InventoryItem.None);

        public static AcquisitionResult Canceled() =>
            new(AcquisitionOutcome.Canceled, -1, InventoryItem.None);
    }

    // Optional UI prompt that mediates a full-inventory acquisition. Game UI
    // registers a handler so InventoryAcquisition stays UI-agnostic and
    // unit-testable.
    public interface IInventoryFullPromptHandler
    {
        void Prompt(TestUnit unit, InventoryItem incoming, Action<int> onChooseDiscardSlot, Action onCancel);
    }

    // Entry point for putting an item into a unit's inventory. If the inventory
    // has room the item is added immediately; otherwise overflow goes silently
    // to the convoy; otherwise the registered prompt handler asks the player
    // which slot to discard (or to cancel).
    public static class InventoryAcquisition
    {
        public static IInventoryFullPromptHandler PromptHandler { get; set; }

        public static void TryAcquireItem(TestUnit unit, InventoryItem incoming, Action<AcquisitionResult> onComplete)
        {
            if (unit == null || incoming.IsEmpty)
            {
                onComplete?.Invoke(AcquisitionResult.Canceled());
                return;
            }

            var inventory = unit.Inventory;
            if (inventory.TryAddItem(incoming, out int slot))
            {
                onComplete?.Invoke(AcquisitionResult.Added(slot));
                return;
            }

            if (Convoy.Current.IsAvailable && Convoy.Current.TryDeposit(incoming))
            {
                onComplete?.Invoke(AcquisitionResult.SentToConvoy());
                return;
            }

            if (PromptHandler == null)
            {
                // No UI wired — degrade gracefully so editor/tests can still call this.
                onComplete?.Invoke(AcquisitionResult.Canceled());
                return;
            }

            PromptHandler.Prompt(unit, incoming,
                onChooseDiscardSlot: discardSlot =>
                {
                    var displaced = inventory.GetSlot(discardSlot);
                    inventory.SetSlot(discardSlot, incoming);
                    onComplete?.Invoke(AcquisitionResult.Swapped(discardSlot, displaced));
                },
                onCancel: () => onComplete?.Invoke(AcquisitionResult.Canceled()));
        }
    }
}
