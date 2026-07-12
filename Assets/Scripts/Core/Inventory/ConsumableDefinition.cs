using UnityEngine;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core
{
    // Designer-authored consumable (potion, stat booster). Bakes into the
    // runtime ConsumableData struct.
    [CreateAssetMenu(menuName = "Project Astra/Items/Consumable", fileName = "NewConsumable")]
    public class ConsumableDefinition : ItemDefinition
    {
        [Header("Effect")]
        [SerializeField] private ConsumableType type;
        [Tooltip("HP healed (Vulnerary) or stat points granted (StatBooster).")]
        [SerializeField] private int magnitude = 10;
        [Tooltip("Stat raised — only used when Type is StatBooster.")]
        [SerializeField] private StatIndex targetStat;

        [Header("Durability")]
        [SerializeField, Min(0)] private int maxUses = 3;

        public ConsumableData ToRuntime() => new()
        {
            name = DisplayName,
            type = type,
            magnitude = magnitude,
            targetStat = targetStat,
            maxUses = maxUses,
            currentUses = maxUses,
        };

        public override InventoryItem ToInventoryItem() => InventoryItem.FromConsumable(ToRuntime());
    }
}
