using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    public enum ConsumableType
    {
        Vulnerary,
        StatBooster,
    }

    [Serializable]
    public struct ConsumableData
    {
        public string name;
        public ConsumableType type;
        public int magnitude;
        public int maxUses;
        public int currentUses;
        public StatIndex targetStat;

        public bool IsDepleted => maxUses > 0 && currentUses <= 0;

        public void ConsumeUse()
        {
            if (maxUses <= 0) return;
            currentUses = Mathf.Max(0, currentUses - 1);
        }

        public static ConsumableData Vulnerary => new()
        {
            name = "Sanjivani",
            type = ConsumableType.Vulnerary,
            magnitude = 10,
            maxUses = 3,
            currentUses = 3,
        };

        public static ConsumableData AmritaVastra => new()
        {
            name = "Amrita Vastra", type = ConsumableType.StatBooster,
            targetStat = StatIndex.HP, magnitude = 7, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData ShaktiMudrika => new()
        {
            name = "Shakti Mudrika", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Str, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData VidyaDhuli => new()
        {
            name = "Vidya Dhuli", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Mag, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData SutraGrantha => new()
        {
            name = "Sutra Grantha", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Skl, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData VayuPankha => new()
        {
            name = "Vayu Pankha", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Spd, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData DeviPratima => new()
        {
            name = "Devi Pratima", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Niyati, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData NagaKavach => new()
        {
            name = "Naga Kavach", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Def, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData RakshaSutra => new()
        {
            name = "Raksha Sutra", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Res, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData DehaMudrika => new()
        {
            name = "Deha Mudrika", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Con, magnitude = 2, maxUses = 1, currentUses = 1,
        };
    }
}
