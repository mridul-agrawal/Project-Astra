using System;
using System.Collections.Generic;

namespace ProjectAstra.Core
{
    // Shared 100-slot supply convoy for the player's army. Items are
    // auto-sorted on every deposit. Plugs into Convoy.Current to replace
    // NullConvoy at scene start.
    public class SupplyConvoy : IConvoy
    {
        public const int MaxCapacity = 100;

        private readonly List<InventoryItem> items = new();
        private readonly ItemSortComparer comparer = new();

        public event Action OnConvoyChanged;

        public bool IsAvailable => true;
        public int Count => items.Count;
        public int Capacity => MaxCapacity;
        public bool IsFull => items.Count >= MaxCapacity;

        public bool TryDeposit(InventoryItem item)
        {
            if (item.IsEmpty || IsFull) return false;
            items.Add(item);
            items.Sort(comparer);
            OnConvoyChanged?.Invoke();
            return true;
        }

        public bool TryWithdraw(int index, out InventoryItem item)
        {
            if (index < 0 || index >= items.Count)
            {
                item = InventoryItem.None;
                return false;
            }
            item = items[index];
            items.RemoveAt(index);
            OnConvoyChanged?.Invoke();
            return true;
        }

        public InventoryItem GetSlot(int index)
        {
            if (index < 0 || index >= items.Count) return InventoryItem.None;
            return items[index];
        }

        public InventoryItem[] ToArray() => items.ToArray();

        public void LoadFrom(InventoryItem[] saved)
        {
            items.Clear();
            if (saved == null) return;
            foreach (var item in saved)
                if (!item.IsEmpty) items.Add(item);
            items.Sort(comparer);
            OnConvoyChanged?.Invoke();
        }
    }
}
