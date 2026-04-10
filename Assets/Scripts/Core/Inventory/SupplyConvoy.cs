using System;
using System.Collections.Generic;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Shared 100-slot supply convoy for the player's army. Items are auto-sorted
    /// on every deposit. Plugs into Convoy.Current to replace NullConvoy.
    /// </summary>
    public class SupplyConvoy : IConvoy
    {
        public const int MaxCapacity = 100;

        private readonly List<InventoryItem> _items = new();
        private readonly ItemSortComparer _comparer = new();

        public event Action OnConvoyChanged;

        public bool IsAvailable => true;
        public int Count => _items.Count;
        public int Capacity => MaxCapacity;
        public bool IsFull => _items.Count >= MaxCapacity;

        public bool TryDeposit(InventoryItem item)
        {
            if (item.IsEmpty || IsFull) return false;
            _items.Add(item);
            _items.Sort(_comparer);
            OnConvoyChanged?.Invoke();
            return true;
        }

        public bool TryWithdraw(int index, out InventoryItem item)
        {
            if (index < 0 || index >= _items.Count)
            {
                item = InventoryItem.None;
                return false;
            }
            item = _items[index];
            _items.RemoveAt(index);
            OnConvoyChanged?.Invoke();
            return true;
        }

        public InventoryItem GetSlot(int index)
        {
            if (index < 0 || index >= _items.Count) return InventoryItem.None;
            return _items[index];
        }

        public InventoryItem[] ToArray() => _items.ToArray();

        public void LoadFrom(InventoryItem[] saved)
        {
            _items.Clear();
            if (saved == null) return;
            foreach (var item in saved)
                if (!item.IsEmpty) _items.Add(item);
            _items.Sort(_comparer);
            OnConvoyChanged?.Invoke();
        }
    }
}
