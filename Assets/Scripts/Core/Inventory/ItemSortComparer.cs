using System.Collections.Generic;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Sorts convoy items by category (weapons by type → staves → consumables),
    /// then by tier/sub-type, then alphabetically by name.
    /// </summary>
    public class ItemSortComparer : IComparer<InventoryItem>
    {
        public int Compare(InventoryItem a, InventoryItem b)
        {
            int catA = CategoryRank(a);
            int catB = CategoryRank(b);
            if (catA != catB) return catA.CompareTo(catB);

            int subA = SubcategoryRank(a);
            int subB = SubcategoryRank(b);
            if (subA != subB) return subA.CompareTo(subB);

            int tierA = TierRank(a);
            int tierB = TierRank(b);
            if (tierA != tierB) return tierA.CompareTo(tierB);

            return string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal);
        }

        private static int CategoryRank(InventoryItem item)
        {
            if (item.IsEmpty) return 99;
            if (item.kind == ItemKind.Weapon)
                return item.weapon.weaponType == WeaponType.Staff ? 1 : 0;
            if (item.kind == ItemKind.Consumable) return 2;
            return 99;
        }

        private static int SubcategoryRank(InventoryItem item)
        {
            if (item.kind == ItemKind.Weapon) return (int)item.weapon.weaponType;
            if (item.kind == ItemKind.Consumable) return (int)item.consumable.type;
            return 99;
        }

        private static int TierRank(InventoryItem item)
        {
            if (item.kind == ItemKind.Weapon) return (int)item.weapon.tier;
            return 0;
        }
    }
}
