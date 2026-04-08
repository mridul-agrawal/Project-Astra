using UnityEngine;

namespace ProjectAstra.Core
{
    public static class CombatEngine
    {
        public static int ComputeAttackerHit(int skill, int niyati, int weaponHit, int wtHitBonus = 0)
        {
            return Mathf.Max(0, skill * 2 + niyati + weaponHit + wtHitBonus);
        }

        public static int ComputeAvoid(int attackSpeed, int niyati, int terrainAvoidBonus)
        {
            return Mathf.Max(0, attackSpeed * 2 + niyati + terrainAvoidBonus);
        }

        public static int ComputeDisplayedHit(int attackerHit, int defenderAvoid)
        {
            return Mathf.Clamp(attackerHit - defenderAvoid, 0, 100);
        }

        public static int ComputePhysicalDamage(int str, int weaponMight, int wtDmgBonus, int def, int terrainDefBonus)
        {
            return Mathf.Max(0, str + weaponMight + wtDmgBonus - def - terrainDefBonus);
        }

        public static int ComputeMagicalDamage(int mag, int weaponMight, int magTriDmgBonus, int res)
        {
            return Mathf.Max(0, mag + weaponMight + magTriDmgBonus - res);
        }

        public static int ComputeCritRate(int skill, int weaponCrit, int classCrit, int defenderNiyati)
        {
            return Mathf.Max(0, skill / 2 + weaponCrit + classCrit - defenderNiyati);
        }

        public static bool CanDoubleAttack(int attackerAS, int defenderAS)
        {
            return attackerAS - defenderAS >= 4;
        }

        public static int RollTrueHit(int rand1, int rand2)
        {
            return (rand1 + rand2 + 1) / 2;
        }

        public static bool IsHit(int displayedHit, int trueHitRoll)
        {
            if (displayedHit >= 100) return true;
            if (displayedHit <= 0) return false;
            return displayedHit > trueHitRoll;
        }

        public static bool IsCrit(int displayedCritRate, int critRoll)
        {
            if (displayedCritRate <= 0) return false;
            return displayedCritRate > critRoll;
        }

        public static int ApplyCritMultiplier(int baseDamage)
        {
            return baseDamage * 3;
        }

        public static int ComputeDamage(DamageType type, int str, int mag, int weaponMight,
            int wtDmgBonus, int def, int res, int terrainDefBonus)
        {
            return type == DamageType.Physical
                ? ComputePhysicalDamage(str, weaponMight, wtDmgBonus, def, terrainDefBonus)
                : ComputeMagicalDamage(mag, weaponMight, wtDmgBonus, res);
        }
    }
}
