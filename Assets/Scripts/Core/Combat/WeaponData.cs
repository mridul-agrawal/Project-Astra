using System;
using UnityEngine;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat
{
    // All the stats and rules for a single weapon instance — including its remaining uses.
    // WeaponData is a value type because every InventoryItem copies the weapon snapshot it carries
    // (durability lives on the copy). Authored weapons live as WeaponDefinition assets and bake into
    // this struct via ToRuntime(); None is the empty sentinel for an unarmed slot.
    [Serializable]
    public struct WeaponData
    {
        public string name;
        public WeaponType weaponType;
        public DamageType damageType;
        public MagicSchool magicSchool;
        public StaffEffect staffEffect;
        public WeaponTier tier;
        public WeaponRank minRank;

        public int might;
        public int hit;
        public int crit;
        public int weight;
        public int minRange;
        public int maxRange;

        public int maxUses;
        public int currentUses;
        public bool indestructible;

        public bool brave;
        public ClassType[] effectivenessTargets;

        public bool characterLocked;
        public string ownerUnitId;

        public bool IsEmpty => string.IsNullOrEmpty(name);
        public bool IsBroken => !indestructible && maxUses > 0 && currentUses <= 0;

        public bool CanReachRange(int distance) => distance >= minRange && distance <= maxRange;

        public bool IsEffectiveAgainst(ClassType target)
        {
            if (effectivenessTargets == null) return false;
            foreach (var t in effectivenessTargets)
                if (t == target) return true;
            return false;
        }

        public void ConsumeDurability(int amount = 1)
        {
            if (indestructible) return;
            if (maxUses <= 0) return;
            currentUses = Mathf.Max(0, currentUses - amount);
        }

        // Empty sentinel — an unarmed slot. Stock weapons now live as WeaponDefinition assets.
        public static WeaponData None => default;
    }
}
