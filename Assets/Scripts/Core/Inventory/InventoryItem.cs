using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    public enum ItemKind
    {
        None,
        Weapon,
        Consumable,
    }

    /// <summary>
    /// Discriminated union of weapon or consumable. Lives in a single inventory slot.
    /// Struct (not class) to mirror WeaponData and avoid GC pressure during inventory scans.
    /// </summary>
    [Serializable]
    public struct InventoryItem
    {
        public ItemKind kind;
        public WeaponData weapon;
        public ConsumableData consumable;

        public bool IsEmpty => kind == ItemKind.None;

        public string DisplayName => kind switch
        {
            ItemKind.Weapon => weapon.name,
            ItemKind.Consumable => consumable.name,
            _ => "",
        };

        public int CurrentUses => kind switch
        {
            ItemKind.Weapon => weapon.currentUses,
            ItemKind.Consumable => consumable.currentUses,
            _ => 0,
        };

        public int MaxUses => kind switch
        {
            ItemKind.Weapon => weapon.maxUses,
            ItemKind.Consumable => consumable.maxUses,
            _ => 0,
        };

        public bool Indestructible => kind == ItemKind.Weapon && weapon.indestructible;

        public bool IsDepleted => kind switch
        {
            ItemKind.Weapon => weapon.IsBroken,
            ItemKind.Consumable => consumable.IsDepleted,
            _ => true,
        };

        public void ConsumeUse()
        {
            if (kind == ItemKind.Weapon) weapon.ConsumeDurability();
            else if (kind == ItemKind.Consumable) consumable.ConsumeUse();
        }

        public static InventoryItem None => default;

        public static InventoryItem FromWeapon(WeaponData w) => new()
        {
            kind = w.IsEmpty ? ItemKind.None : ItemKind.Weapon,
            weapon = w,
        };

        public static InventoryItem FromConsumable(ConsumableData c) => new()
        {
            kind = string.IsNullOrEmpty(c.name) ? ItemKind.None : ItemKind.Consumable,
            consumable = c,
        };
    }
}
