using System;
using UnityEngine;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Stats
{
    // All the stat math, as pure functions. Level-up rolls, attack speed, HP and Niyati thresholds, rescue.
    public static class StatUtils
    {
        const int DefaultStatGain = 1;
        const int PercentScale = 100;

        // FE-style HP color thresholds (percent of max).
        const int InjuredMaxPercent = 50;
        const int CriticalMaxPercent = 30;

        // Niyati lotus icon thresholds (current / base ratio).
        const float LotusFullMinRatio = 1.2f;
        const float LotusHalfMinRatio = 0.8f;

        // A unit can rescue another whose CON is at most this many points above its own.
        const int RescueConDifferenceLimit = 10;

        public static StatArray ComputeLevelUpGains(StatArray personalGrowths, StatArray classModifiers, StatArray currentStats,
            StatArray caps, int hpGainOnLevelUp, Func<int, bool> rollFunction)
        {
            var gains = StatArray.Create();

            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                if (idx == StatIndex.Con) continue; // Con doesn't grow on level-up.

                int growth = EffectiveGrowth(personalGrowths[idx], classModifiers[idx]);
                if (!rollFunction(growth)) continue;

                int gain = (idx == StatIndex.HP) ? hpGainOnLevelUp : DefaultStatGain;
                if (currentStats[idx] + gain > caps[idx]) continue;

                gains[idx] = gain;
            }

            return gains;
        }

        public static void ClampStatsToCaps(ref StatArray stats, StatArray caps)
        {
            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                stats[idx] = Mathf.Min(stats[idx], caps[idx]);
            }
        }

        public static int EffectiveGrowth(int personalGrowth, int classModifier) => personalGrowth + classModifier;

        public static bool RollGrowth(int growthRate, int roll) => roll < growthRate;

        public static int AttackSpeed(int speed, int weaponWeight, int constitution) => Mathf.Max(0, speed - WeightPenalty(weaponWeight, constitution));

        public static int WeightPenalty(int weaponWeight, int constitution) => Mathf.Max(0, weaponWeight - constitution);

        public static HPThreshold CalculateHPThreshold(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return HPThreshold.Critical;
            int percent = currentHP * PercentScale / maxHP;
            if (percent > InjuredMaxPercent) return HPThreshold.Normal;
            if (percent > CriticalMaxPercent) return HPThreshold.Injured;
            return HPThreshold.Critical;
        }

        public static NiyatiSymbol CalculateNiyatiSymbol(int currentNiyati, int baseNiyati)
        {
            if (baseNiyati <= 0) return NiyatiSymbol.LotusHalf;
            float ratio = (float)currentNiyati / baseNiyati;
            // Both endpoints (0.8 and 1.2) fall in LotusHalf — the band is inclusive.
            if (ratio > LotusFullMinRatio) return NiyatiSymbol.LotusFull;
            if (ratio >= LotusHalfMinRatio) return NiyatiSymbol.LotusHalf;
            return NiyatiSymbol.LotusWithered;
        }

        public static bool CanRescue(int rescuerCon, int rescuedCon) => rescuerCon >= rescuedCon - RescueConDifferenceLimit;
    }
}
