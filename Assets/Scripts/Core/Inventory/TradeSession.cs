namespace ProjectAstra.Core
{
    /// <summary>
    /// Manages a trade between two units by operating on deep copies of their inventories.
    /// All swap/give/take operations mutate the working copies only. Call Commit() to
    /// apply changes to the real inventories, or simply discard this object to cancel.
    /// </summary>
    public class TradeSession
    {
        public const int Capacity = UnitInventory.Capacity;

        private readonly InventoryItem[] _leftSlots;
        private readonly InventoryItem[] _rightSlots;
        private readonly InventoryItem[] _leftOriginal;
        private readonly InventoryItem[] _rightOriginal;

        public TestUnit LeftUnit { get; }
        public TestUnit RightUnit { get; }

        public TradeSession(TestUnit initiator, TestUnit target)
        {
            LeftUnit = initiator;
            RightUnit = target;

            _leftSlots = new InventoryItem[Capacity];
            _rightSlots = new InventoryItem[Capacity];
            _leftOriginal = new InventoryItem[Capacity];
            _rightOriginal = new InventoryItem[Capacity];

            for (int i = 0; i < Capacity; i++)
            {
                _leftSlots[i] = initiator.Inventory.GetSlot(i);
                _rightSlots[i] = target.Inventory.GetSlot(i);
                _leftOriginal[i] = _leftSlots[i];
                _rightOriginal[i] = _rightSlots[i];
            }
        }

        public InventoryItem GetLeftSlot(int index) =>
            index >= 0 && index < Capacity ? _leftSlots[index] : InventoryItem.None;

        public InventoryItem GetRightSlot(int index) =>
            index >= 0 && index < Capacity ? _rightSlots[index] : InventoryItem.None;

        public bool HasChanges
        {
            get
            {
                for (int i = 0; i < Capacity; i++)
                {
                    if (_leftSlots[i].kind != _leftOriginal[i].kind) return true;
                    if (_leftSlots[i].DisplayName != _leftOriginal[i].DisplayName) return true;
                    if (_rightSlots[i].kind != _rightOriginal[i].kind) return true;
                    if (_rightSlots[i].DisplayName != _rightOriginal[i].DisplayName) return true;
                }
                return false;
            }
        }

        #region Operations

        public bool CanSwap(int leftSlot, int rightSlot)
        {
            if (!ValidSlot(leftSlot) || !ValidSlot(rightSlot)) return false;
            return !_leftSlots[leftSlot].IsEmpty && !_rightSlots[rightSlot].IsEmpty;
        }

        public bool TrySwap(int leftSlot, int rightSlot)
        {
            if (!CanSwap(leftSlot, rightSlot)) return false;
            (_leftSlots[leftSlot], _rightSlots[rightSlot]) = (_rightSlots[rightSlot], _leftSlots[leftSlot]);
            return true;
        }

        public bool CanGive(int leftSlot)
        {
            if (!ValidSlot(leftSlot)) return false;
            if (_leftSlots[leftSlot].IsEmpty) return false;
            return FirstEmpty(_rightSlots) >= 0;
        }

        public bool TryGive(int leftSlot)
        {
            if (!CanGive(leftSlot)) return false;
            int emptyRight = FirstEmpty(_rightSlots);
            _rightSlots[emptyRight] = _leftSlots[leftSlot];
            _leftSlots[leftSlot] = InventoryItem.None;
            return true;
        }

        public bool CanTake(int rightSlot)
        {
            if (!ValidSlot(rightSlot)) return false;
            if (_rightSlots[rightSlot].IsEmpty) return false;
            return FirstEmpty(_leftSlots) >= 0;
        }

        public bool TryTake(int rightSlot)
        {
            if (!CanTake(rightSlot)) return false;
            int emptyLeft = FirstEmpty(_leftSlots);
            _leftSlots[emptyLeft] = _rightSlots[rightSlot];
            _rightSlots[rightSlot] = InventoryItem.None;
            return true;
        }

        #endregion

        public void Commit()
        {
            var leftInv = LeftUnit.Inventory;
            var rightInv = RightUnit.Inventory;
            for (int i = 0; i < Capacity; i++)
            {
                leftInv.SetSlot(i, _leftSlots[i]);
                rightInv.SetSlot(i, _rightSlots[i]);
            }
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
