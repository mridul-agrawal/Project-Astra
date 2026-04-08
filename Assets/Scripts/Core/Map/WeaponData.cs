using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [Serializable]
    public struct WeaponData
    {
        public string name;
        public WeaponType weaponType;
        public DamageType damageType;
        public int might;
        public int hit;
        public int crit;
        public int weight;
        public int minRange;
        public int maxRange;

        public bool IsEmpty => string.IsNullOrEmpty(name);

        public bool CanReachRange(int distance)
        {
            return distance >= minRange && distance <= maxRange;
        }

        public static WeaponData None => default;

        public static WeaponData IronSword => new()
        {
            name = "Iron Sword", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            might = 5, hit = 90, crit = 0, weight = 5, minRange = 1, maxRange = 1
        };

        public static WeaponData IronLance => new()
        {
            name = "Iron Lance", weaponType = WeaponType.Lance, damageType = DamageType.Physical,
            might = 7, hit = 80, crit = 0, weight = 8, minRange = 1, maxRange = 1
        };

        public static WeaponData Fire => new()
        {
            name = "Fire", weaponType = WeaponType.AnimaTome, damageType = DamageType.Magical,
            might = 5, hit = 90, crit = 0, weight = 4, minRange = 1, maxRange = 2
        };

        public static WeaponData Heal => new()
        {
            name = "Heal", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            might = 0, hit = 100, crit = 0, weight = 2, minRange = 1, maxRange = 1
        };
    }
}
