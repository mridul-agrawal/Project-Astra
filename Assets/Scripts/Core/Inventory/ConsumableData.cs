using System;
using UnityEngine;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core
{
    // What a consumable does on use. Stored on item assets and inventory slots as the integer
    // value; don't reorder.
    public enum ConsumableType
    {
        Vulnerary,
        StatBooster,
    }

    // Single-slot consumable item. Value type so each InventoryItem owns its own use counter —
    // copies of the same consumable deplete independently. Authored consumables live as
    // ConsumableDefinition assets and bake into this struct via ToRuntime().
    [Serializable]
    public struct ConsumableData
    {
        public string name;
        public ConsumableType type;
        public int magnitude;
        public int maxUses;
        public int currentUses;
        public StatIndex targetStat;

        public bool IsDepleted => maxUses > 0 && currentUses <= 0;

        public void ConsumeUse()
        {
            if (maxUses <= 0) return;
            currentUses = Mathf.Max(0, currentUses - 1);
        }
    }
}
