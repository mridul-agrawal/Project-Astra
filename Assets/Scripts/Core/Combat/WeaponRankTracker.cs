using System;
using System.Collections.Generic;

namespace ProjectAstra.Core.Combat
{
    // Per-unit weapon proficiency tracker. Holds the current rank in each
    // weapon type the unit has access to, plus accumulated WEXP toward the
    // next rank. Rank-ups fire OnRankUp.
    public class WeaponRankTracker
    {
        // FE GBA WEXP needed to reach the next rank (E→D, D→C, C→B, B→A, A→S).
        private static readonly int[] WexpThresholds = { 1, 40, 71, 121, 201 };

        private readonly Dictionary<WeaponType, WeaponRank> ranks = new();
        private readonly Dictionary<WeaponType, int> wexp = new();

        public event Action<WeaponType, WeaponRank> OnRankUp;

        public void InitializeRank(WeaponType type, WeaponRank startingRank)
        {
            ranks[type] = startingRank;
            wexp[type] = 0;
        }

        public WeaponRank GetRank(WeaponType type)
        {
            return ranks.TryGetValue(type, out var rank) ? rank : WeaponRank.E;
        }

        public int GetWexp(WeaponType type)
        {
            return this.wexp.TryGetValue(type, out var wexp) ? wexp : 0;
        }

        public bool HasAccess(WeaponType type) => ranks.ContainsKey(type);

        public bool CanEquip(WeaponData weapon)
        {
            if (!HasAccess(weapon.weaponType)) return false;
            return GetRank(weapon.weaponType) >= weapon.minRank;
        }

        public void AddWexp(WeaponType type, int amount = 1)
        {
            if (!ranks.ContainsKey(type)) return;

            var rank = ranks[type];
            if (rank >= WeaponRank.S) return;

            if (!wexp.ContainsKey(type)) wexp[type] = 0;
            wexp[type] += amount;

            int thresholdIndex = (int)rank;
            if (thresholdIndex < WexpThresholds.Length && wexp[type] >= WexpThresholds[thresholdIndex])
            {
                wexp[type] = 0;
                ranks[type] = rank + 1;
                OnRankUp?.Invoke(type, ranks[type]);
            }
        }

        public static int GetThreshold(WeaponRank currentRank)
        {
            int index = (int)currentRank;
            return index < WexpThresholds.Length ? WexpThresholds[index] : int.MaxValue;
        }
    }
}
