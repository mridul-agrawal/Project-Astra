using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public static class StaffEffects
    {
        public static bool CanUseStaff(TestUnit healer, WeaponData staff, out string failReason)
        {
            failReason = null;
            if (healer == null)
            {
                failReason = "No healer unit.";
                return false;
            }
            if (staff.weaponType != WeaponType.Staff || staff.staffEffect == StaffEffect.None)
            {
                failReason = "Not a healing staff.";
                return false;
            }
            if (staff.IsBroken)
            {
                failReason = "Staff is broken.";
                return false;
            }
            return true;
        }

        public static bool CanHealTarget(
            TestUnit healer, TestUnit target, WeaponData staff, int magStat, out string failReason)
        {
            failReason = null;
            if (target == null)
            {
                failReason = "No target unit.";
                return false;
            }
            if (healer == target)
            {
                failReason = "Cannot self-heal with a staff.";
                return false;
            }
            if (target.faction != healer.faction && target.faction != Faction.Allied)
            {
                failReason = "Can only heal allied units.";
                return false;
            }

            int targetHP, targetMaxHP;
            if (target.UnitInstance != null)
            {
                targetHP = target.UnitInstance.CurrentHP;
                targetMaxHP = target.UnitInstance.MaxHP;
            }
            else
            {
                targetHP = target.currentHP;
                targetMaxHP = target.maxHP;
            }

            if (targetHP >= targetMaxHP)
            {
                failReason = "Target is already at full HP.";
                return false;
            }

            if (!StaffRangeResolver.IsInRange(staff, magStat, healer.gridPosition, target.gridPosition))
            {
                failReason = "Target is out of range.";
                return false;
            }

            return true;
        }

        public static int ComputeHealAmount(
            WeaponData staff, int magStat, int targetCurrentHP, int targetMaxHP)
        {
            int missingHP = targetMaxHP - targetCurrentHP;
            if (missingHP <= 0) return 0;

            switch (staff.staffEffect)
            {
                case StaffEffect.FullHeal:
                    return missingHP;

                case StaffEffect.Ranged:
                case StaffEffect.AreaOfEffect:
                    return Mathf.Min(staff.might / 2 + magStat, missingHP);

                case StaffEffect.Heal:
                default:
                    return Mathf.Min(staff.might + magStat, missingHP);
            }
        }

        // Offensive-staff hit formula (Sleep, Silence): per FE GBA convention.
        // Keep this here so CombatForecastUI + later runtime use a single source.
        public static int ComputeStaffHit(int casterMag, int casterSkl, int targetRes)
        {
            return Mathf.Clamp(30 + casterMag * 5 + casterSkl - targetRes * 5, 0, 100);
        }

        public static bool ApplyHeal(
            TestUnit healer, TestUnit target, ref WeaponData staff,
            out int amountHealed, out string failReason)
        {
            amountHealed = 0;
            if (!CanUseStaff(healer, staff, out failReason))
                return false;
            if (!CanHealTarget(healer, target, staff, GetMag(healer), out failReason))
                return false;

            int currentHP, maxHP;
            GetHP(target, out currentHP, out maxHP);

            amountHealed = ComputeHealAmount(staff, GetMag(healer), currentHP, maxHP);
            ApplyHealing(target, amountHealed);
            staff.ConsumeDurability();

            GrantHealExp(healer, target);

            return true;
        }

        public static bool ApplyFortify(
            TestUnit healer, List<TestUnit> allUnits, ref WeaponData staff,
            out List<(TestUnit unit, int amount)> healed, out string failReason)
        {
            healed = new List<(TestUnit, int)>();
            if (!CanUseStaff(healer, staff, out failReason))
                return false;

            int mag = GetMag(healer);

            foreach (var unit in allUnits)
            {
                if (unit == null) continue;
                if (unit.faction != healer.faction && unit.faction != Faction.Allied) continue;

                if (!StaffRangeResolver.IsInRange(staff, mag, healer.gridPosition, unit.gridPosition))
                    continue;

                int currentHP, maxHP;
                GetHP(unit, out currentHP, out maxHP);
                if (currentHP >= maxHP) continue;

                int amount = ComputeHealAmount(staff, mag, currentHP, maxHP);
                if (amount <= 0) continue;

                ApplyHealing(unit, amount);
                healed.Add((unit, amount));
            }

            if (healed.Count == 0)
            {
                failReason = "No valid targets to heal.";
                return false;
            }

            staff.ConsumeDurability();

            foreach (var (unit, _) in healed)
                GrantHealExp(healer, unit);

            return true;
        }

        private static void GrantHealExp(TestUnit healer, TestUnit healed)
        {
            if (ExpGranter.Instance == null) return;
            if (healer == null || healed == null) return;
            if (healer.UnitInstance == null || healed.UnitInstance == null) return;

            int exp = ExpMath.ComputeHealExp(healer.UnitInstance, healed.UnitInstance);
            ExpGranter.Instance.Grant(healer, exp);
        }

        private static int GetMag(TestUnit unit)
        {
            return unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;
        }

        private static void GetHP(TestUnit unit, out int currentHP, out int maxHP)
        {
            if (unit.UnitInstance != null)
            {
                currentHP = unit.UnitInstance.CurrentHP;
                maxHP = unit.UnitInstance.MaxHP;
            }
            else
            {
                currentHP = unit.currentHP;
                maxHP = unit.maxHP;
            }
        }

        private static void ApplyHealing(TestUnit unit, int amount)
        {
            if (unit.UnitInstance != null)
                unit.UnitInstance.ApplyHealing(amount);
            else
                unit.currentHP = Mathf.Min(unit.maxHP, unit.currentHP + amount);
        }

        // TODO: USE-01 — when rescue system exists, allow targeting carrier's tile to heal carried unit
    }
}
