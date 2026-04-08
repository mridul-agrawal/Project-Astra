using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    public static class StatUtils
    {
        public const int DefaultHPGainOnLevelUp = 2;

        public static bool RollGrowth(int growthRate, int roll)
        {
            return roll < growthRate;
        }

        public static int EffectiveGrowth(int personalGrowth, int classModifier)
        {
            return personalGrowth + classModifier;
        }

        public static int AttackSpeed(int speed, int weaponWeight, int constitution)
        {
            return Mathf.Max(0, speed - Mathf.Max(0, weaponWeight - constitution));
        }

        public static int WeightPenalty(int weaponWeight, int constitution)
        {
            return Mathf.Max(0, weaponWeight - constitution);
        }

        public static HPThreshold CalculateHPThreshold(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return HPThreshold.Critical;
            int percent = currentHP * 100 / maxHP;
            if (percent > 50) return HPThreshold.Normal;
            if (percent >= 31) return HPThreshold.Injured;
            return HPThreshold.Critical;
        }

        public static NiyatiSymbol CalculateNiyatiSymbol(int currentNiyati, int baseNiyati)
        {
            if (baseNiyati <= 0) return NiyatiSymbol.LotusHalf;
            float ratio = (float)currentNiyati / baseNiyati;
            if (ratio > 1.2f) return NiyatiSymbol.LotusFull;
            if (ratio >= 0.8f) return NiyatiSymbol.LotusHalf;
            return NiyatiSymbol.LotusWithered;
        }

        public static bool CanRescue(int rescuerCon, int rescuedCon)
        {
            return rescuerCon >= rescuedCon - 10;
        }

        public static StatArray ComputeLevelUpGains(
            StatArray personalGrowths,
            StatArray classModifiers,
            StatArray currentStats,
            StatArray caps,
            int hpGainOnLevelUp,
            Func<int, int, bool> rollFunction)
        {
            var gains = StatArray.Create();

            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;

                if (idx == StatIndex.Con) continue;

                int effectiveGrowth = EffectiveGrowth(personalGrowths[idx], classModifiers[idx]);
                if (!rollFunction(effectiveGrowth, 0)) continue;

                int gain = (idx == StatIndex.HP) ? hpGainOnLevelUp : 1;
                int newValue = currentStats[idx] + gain;
                if (newValue > caps[idx]) continue;

                gains[idx] = gain;
            }

            return gains;
        }

        public static void ClampStatsToCaps(ref StatArray stats, StatArray caps)
        {
            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                if (stats[idx] > caps[idx])
                    stats[idx] = caps[idx];
            }
        }
    }
}
