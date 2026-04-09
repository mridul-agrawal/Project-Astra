using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    public enum ConsumableType
    {
        Vulnerary,
        // Future: Elixir, StatBooster, PromotionItem
    }

    [Serializable]
    public struct ConsumableData
    {
        public string name;
        public ConsumableType type;
        public int magnitude;
        public int maxUses;
        public int currentUses;

        public bool IsDepleted => maxUses > 0 && currentUses <= 0;

        public void ConsumeUse()
        {
            if (maxUses <= 0) return;
            currentUses = Mathf.Max(0, currentUses - 1);
        }

        public static ConsumableData Vulnerary => new()
        {
            name = "Sanjivani",
            type = ConsumableType.Vulnerary,
            magnitude = 10,
            maxUses = 3,
            currentUses = 3,
        };
    }
}
