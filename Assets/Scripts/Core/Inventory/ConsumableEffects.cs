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
                default:
                    failReason = $"Unknown consumable type: {consumable.type}";
                    return false;
            }
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
    }
}
