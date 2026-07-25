using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Procedural item/weapon description text for the gear footer + inspect overlay.
    // (Phase 3 will enrich this from the InventoryMenuUI DescribeWeapon/DescribeConsumable logic.)
    public static class ItemDescriber
    {
        public static string Describe(InventoryItem item)
        {
            switch (item.kind)
            {
                case ItemKind.Weapon:     return DescribeWeapon(item.weapon);
                case ItemKind.Consumable: return DescribeConsumable(item.consumable);
                default:                  return "";
            }
        }

        private static string DescribeWeapon(WeaponData w) =>
            $"{w.weaponType} · Mt {w.might} · Hit {w.hit} · Rng {Range(w.minRange, w.maxRange)}";

        private static string DescribeConsumable(ConsumableData c) => c.type.ToString();

        private static string Range(int min, int max) => min == max ? min.ToString() : min + "-" + max;
    }
}
