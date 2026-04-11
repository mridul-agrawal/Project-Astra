using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Applies consumable effects to a unit. Routes by ConsumableType so new consumables
    /// can be added without touching UnitInventory.
    /// </summary>
    public static class ConsumableEffects
    {
        public static bool Apply(ConsumableData consumable, TestUnit user, out string failReason)
        {
            failReason = null;
            if (user == null)
            {
                failReason = "No target unit.";
                return false;
            }

            switch (consumable.type)
            {
                case ConsumableType.Vulnerary:
                    return ApplyVulnerary(consumable, user, out failReason);
                case ConsumableType.StatBooster:
                    return ApplyStatBooster(consumable, user, out failReason);
                default:
                    failReason = $"Unknown consumable type: {consumable.type}";
                    return false;
            }
        }

        public static (string message, bool reducedEffect) DescribeStatBoost(
            ConsumableData data, TestUnit unit)
        {
            string statName = StatNameFor(data.targetStat);
            string unitName = unit != null ? unit.name : "unit";

            if (unit?.UnitInstance == null)
                return ($"Use {data.name} on {unitName}? {statName} +{data.magnitude}.", false);

            int current = unit.UnitInstance.Stats[data.targetStat];
            int cap = unit.UnitInstance.CurrentClass != null
                ? unit.UnitInstance.CurrentClass.StatCaps[data.targetStat]
                : int.MaxValue;
            int effectiveGain = Mathf.Min(data.magnitude, Mathf.Max(0, cap - current));

            string msg = $"Use {data.name} on {unitName}?\n{statName} will increase by {effectiveGain}.";

            if (effectiveGain <= 0)
            {
                msg += $"\n{unitName}'s {statName} is at its class maximum. This item will have no effect.";
                return (msg, true);
            }
            if (effectiveGain < data.magnitude)
            {
                msg += $"\n{unitName}'s {statName} is near its class maximum. Effect will be reduced.";
                return (msg, true);
            }

            return (msg, false);
        }

        public static string StatNameFor(StatIndex stat)
        {
            return stat switch
            {
                StatIndex.HP => "HP",
                StatIndex.Str => "Strength",
                StatIndex.Mag => "Magic",
                StatIndex.Skl => "Skill",
                StatIndex.Spd => "Speed",
                StatIndex.Def => "Defense",
                StatIndex.Res => "Resistance",
                StatIndex.Con => "Constitution",
                StatIndex.Niyati => "Niyati",
                _ => stat.ToString(),
            };
        }

        private static bool ApplyVulnerary(ConsumableData consumable, TestUnit user, out string failReason)
        {
            failReason = null;
            int magnitude = Mathf.Max(0, consumable.magnitude);

            if (user.UnitInstance != null)
            {
                if (user.UnitInstance.CurrentHP >= user.UnitInstance.MaxHP)
                {
                    failReason = "HP already full.";
                    return false;
                }
                user.UnitInstance.ApplyHealing(magnitude);
                return true;
            }

            if (user.currentHP >= user.maxHP)
            {
                failReason = "HP already full.";
                return false;
            }
            user.currentHP = Mathf.Min(user.maxHP, user.currentHP + magnitude);
            return true;
        }

        private static bool ApplyStatBooster(ConsumableData consumable, TestUnit user, out string failReason)
        {
            failReason = null;
            if (user.UnitInstance == null)
            {
                failReason = "Requires unit data to apply stat booster.";
                return false;
            }

            user.UnitInstance.ApplyStatBoost(consumable.targetStat, consumable.magnitude);
            return true;
        }
    }
}
