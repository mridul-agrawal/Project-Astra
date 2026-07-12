using ProjectAstra.Core.Units;

namespace ProjectAstra.Core
{
    // Manages a trade between two units by operating on deep copies of their
    // inventories. Swap/give/take only mutate the working copies — call
    // Commit() to apply changes to the real inventories, or discard this
    // object to cancel.
    public class TradeSession
    {
        public const int Capacity = UnitInventory.Capacity;

        public TestUnit LeftUnit { get; }
        public TestUnit RightUnit { get; }

        private readonly InventoryItem[] leftSlots;
        private readonly InventoryItem[] rightSlots;
        private readonly InventoryItem[] leftOriginal;
        private readonly InventoryItem[] rightOriginal;

        public TradeSession(TestUnit initiator, TestUnit target)
        {
            LeftUnit = initiator;
            RightUnit = target;

            leftSlots = new InventoryItem[Capacity];
            rightSlots = new InventoryItem[Capacity];
            leftOriginal = new InventoryItem[Capacity];
            rightOriginal = new InventoryItem[Capacity];

            for (int i = 0; i < Capacity; i++)
            {
                leftSlots[i] = initiator.Inventory.GetSlot(i);
                rightSlots[i] = target.Inventory.GetSlot(i);
                leftOriginal[i] = leftSlots[i];
                rightOriginal[i] = rightSlots[i];
            }
        }

        public InventoryItem GetLeftSlot(int index) =>
            index >= 0 && index < Capacity ? leftSlots[index] : InventoryItem.None;

        public InventoryItem GetRightSlot(int index) =>
            index >= 0 && index < Capacity ? rightSlots[index] : InventoryItem.None;

        public bool HasChanges
        {
            get
            {
                for (int i = 0; i < Capacity; i++)
                {
                    if (SlotDiffers(leftSlots[i], leftOriginal[i])) return true;
                    if (SlotDiffers(rightSlots[i], rightOriginal[i])) return true;
                }
                return false;
            }
        }

        // --- Trade operations ---

        public bool CanSwap(int leftSlot, int rightSlot)
        {
            if (!ValidSlot(leftSlot) || !ValidSlot(rightSlot)) return false;
            return !leftSlots[leftSlot].IsEmpty && !rightSlots[rightSlot].IsEmpty;
        }

        public bool TrySwap(int leftSlot, int rightSlot)
        {
            if (!CanSwap(leftSlot, rightSlot)) return false;
            (leftSlots[leftSlot], rightSlots[rightSlot]) = (rightSlots[rightSlot], leftSlots[leftSlot]);
            return true;
        }

        public bool CanGive(int leftSlot)
        {
            if (!ValidSlot(leftSlot)) return false;
            if (leftSlots[leftSlot].IsEmpty) return false;
            return FirstEmpty(rightSlots) >= 0;
        }

        public bool TryGive(int leftSlot)
        {
            if (!CanGive(leftSlot)) return false;
            int emptyRight = FirstEmpty(rightSlots);
            rightSlots[emptyRight] = leftSlots[leftSlot];
            leftSlots[leftSlot] = InventoryItem.None;
            return true;
        }

        public bool CanTake(int rightSlot)
        {
            if (!ValidSlot(rightSlot)) return false;
            if (rightSlots[rightSlot].IsEmpty) return false;
            return FirstEmpty(leftSlots) >= 0;
        }

        public bool TryTake(int rightSlot)
        {
            if (!CanTake(rightSlot)) return false;
            int emptyLeft = FirstEmpty(leftSlots);
            leftSlots[emptyLeft] = rightSlots[rightSlot];
            rightSlots[rightSlot] = InventoryItem.None;
            return true;
        }

        public void Commit()
        {
            var leftInv = LeftUnit.Inventory;
            var rightInv = RightUnit.Inventory;
            for (int i = 0; i < Capacity; i++)
            {
                leftInv.SetSlot(i, leftSlots[i]);
                rightInv.SetSlot(i, rightSlots[i]);
            }
        }

        private static bool SlotDiffers(InventoryItem a, InventoryItem b)
        {
            return a.kind != b.kind || a.DisplayName != b.DisplayName;
        }

        private static bool ValidSlot(int index) => index >= 0 && index < Capacity;

        private static int FirstEmpty(InventoryItem[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }
    }
}
