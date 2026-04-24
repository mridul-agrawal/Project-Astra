using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// FE GBA experience formulas. Stateless — pure math on UnitInstance inputs.
    ///
    /// Base combat EXP = (31 + defender.EffectiveLevel − attacker.EffectiveLevel) / attacker.ClassPowerFactor.
    /// Kill bonus = 20 + defender.EffectiveLevel − attacker.EffectiveLevel (+20 if defender is promoted).
    /// Both are summed for a kill, and clamped to [1, 100].
    ///
    /// Heal EXP uses the same shape with the healed ally's level standing in for the enemy's.
    /// </summary>
    public static class ExpMath
    {
        public const int MinExpPerAction = 1;
        public const int MaxExpPerAction = 100;
        public const int PromotedEnemyKillBonus = 20;

        public static int ComputeCombatExp(UnitInstance attacker, UnitInstance defender, bool killed)
        {
            if (attacker == null || defender == null || attacker.CurrentClass == null || defender.CurrentClass == null)
                return MinExpPerAction;

            float powerFactor = Mathf.Max(0.1f, attacker.CurrentClass.ExpPowerFactor);
            int levelDelta = defender.EffectiveLevel - attacker.EffectiveLevel;

            int exp = Mathf.RoundToInt((31f + levelDelta) / powerFactor);

            if (killed)
            {
                int killBonus = 20 + levelDelta;
                if (defender.CurrentClass.IsPromoted) killBonus += PromotedEnemyKillBonus;
                exp += killBonus;
            }

            return Mathf.Clamp(exp, MinExpPerAction, MaxExpPerAction);
        }

        public static int ComputeHealExp(UnitInstance healer, UnitInstance healed)
        {
            if (healer == null || healed == null || healer.CurrentClass == null)
                return MinExpPerAction;

            float powerFactor = Mathf.Max(0.1f, healer.CurrentClass.ExpPowerFactor);
            int exp = Mathf.RoundToInt((31f + healed.EffectiveLevel - healer.EffectiveLevel) / powerFactor);

            return Mathf.Clamp(exp, MinExpPerAction, MaxExpPerAction);
        }
    }
}
