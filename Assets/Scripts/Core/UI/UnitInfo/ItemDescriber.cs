using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Item copy for the §7 footer, which wants a short summary on line one and the longer
    // detail on line two, plus the short effect string the §9 chip row shows for consumables.
    public static class ItemDescriber
    {
        public static string Describe(InventoryItem item) => Summary(item);

        public static string Summary(InventoryItem item)
        {
            switch (item.kind)
            {
                case ItemKind.Weapon:     return WeaponSummary(item.weapon);
                case ItemKind.Consumable: return ConsumableSummary(item.consumable);
                default:                  return "An empty slot.";
            }
        }

        public static string Detail(InventoryItem item)
        {
            switch (item.kind)
            {
                case ItemKind.Weapon:     return WeaponDetail(item.weapon);
                case ItemKind.Consumable: return UsesLine(item.CurrentUses, item.MaxUses);
                default:                  return "Nothing is carried here.";
            }
        }

        public static string Effect(ConsumableData c) => c.type switch
        {
            ConsumableType.Vulnerary   => "HEAL " + c.magnitude,
            ConsumableType.StatBooster => "+" + c.magnitude + " " + c.targetStat.ToString().ToUpper(),
            _                          => c.type.ToString().ToUpper(),
        };

        private static string WeaponSummary(WeaponData w) =>
            $"{Titled(w.weaponType.ToString())} at range {Range(w.minRange, w.maxRange)}.";

        private static string WeaponDetail(WeaponData w) =>
            $"Might {w.might} · Hit {w.hit} · Crit {w.crit} · Weight {w.weight}. " +
            UsesLine(w.currentUses, w.indestructible ? 0 : w.maxUses);

        private static string ConsumableSummary(ConsumableData c) => c.type switch
        {
            ConsumableType.Vulnerary   => $"Restores {c.magnitude} HP to the user.",
            ConsumableType.StatBooster => $"Permanently raises {c.targetStat.ToString().ToUpper()} by {c.magnitude}.",
            _                          => Titled(c.type.ToString()) + ".",
        };

        private static string UsesLine(int current, int max) =>
            max > 0 ? $"{current} of {max} uses left." : "Never wears out.";

        private static string Range(int min, int max) => min == max ? min.ToString() : min + "-" + max;

        private static string Titled(string value) =>
            string.IsNullOrEmpty(value) ? "" : char.ToUpper(value[0]) + value.Substring(1).ToLower();
    }
}
