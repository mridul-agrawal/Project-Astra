using System;
using System.Collections.Generic;

namespace ProjectAstra.Core
{
    public class WeaponRankTracker
    {
        private static readonly int[] WexpThresholds = { 1, 40, 71, 121, 201 };

        private readonly Dictionary<WeaponType, WeaponRank> _ranks = new();
        private readonly Dictionary<WeaponType, int> _wexp = new();

        public event Action<WeaponType, WeaponRank> OnRankUp;

        public void InitializeRank(WeaponType type, WeaponRank startingRank)
        {
            _ranks[type] = startingRank;
            _wexp[type] = 0;
        }

        public WeaponRank GetRank(WeaponType type)
        {
            return _ranks.TryGetValue(type, out var rank) ? rank : WeaponRank.E;
        }

        public int GetWexp(WeaponType type)
        {
            return _wexp.TryGetValue(type, out var wexp) ? wexp : 0;
        }

        public bool HasAccess(WeaponType type)
        {
            return _ranks.ContainsKey(type);
        }

        public bool CanEquip(WeaponData weapon)
        {
            if (!HasAccess(weapon.weaponType)) return false;
            return GetRank(weapon.weaponType) >= weapon.minRank;
        }

        public void AddWexp(WeaponType type, int amount = 1)
        {
            if (!_ranks.ContainsKey(type)) return;

            var rank = _ranks[type];
            if (rank >= WeaponRank.S) return;

            if (!_wexp.ContainsKey(type)) _wexp[type] = 0;
            _wexp[type] += amount;

            int thresholdIndex = (int)rank;
            if (thresholdIndex < WexpThresholds.Length && _wexp[type] >= WexpThresholds[thresholdIndex])
            {
                _wexp[type] = 0;
                _ranks[type] = rank + 1;
                OnRankUp?.Invoke(type, _ranks[type]);
            }
        }

        public static int GetThreshold(WeaponRank currentRank)
        {
            int index = (int)currentRank;
            return index < WexpThresholds.Length ? WexpThresholds[index] : int.MaxValue;
        }
    }
}
