using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core
{
    // Staff usage rules + heal arithmetic. Owns the can-use / can-target /
    // amount-healed checks for the Heal, Mend, Recover, Physic, and Fortify
    // staff effects, plus the offensive staff hit formula used by the
    // forecast UI.
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

            GetHP(target, out int targetHP, out int targetMaxHP);
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

            return staff.staffEffect switch
            {
                StaffEffect.FullHeal => missingHP,
                StaffEffect.Ranged or StaffEffect.AreaOfEffect => Mathf.Min(staff.might / 2 + magStat, missingHP),
                _ => Mathf.Min(staff.might + magStat, missingHP), // Heal (and any future variants).
            };
        }

        // Offensive-staff hit formula (Sleep, Silence). FE GBA convention;
        // shared by CombatForecastUI today and the runtime once those staves
        // land.
        public static int ComputeStaffHit(int casterMag, int casterSkl, int targetRes)
        {
            return Mathf.Clamp(30 + casterMag * 5 + casterSkl - targetRes * 5, 0, 100);
        }

        public static bool ApplyHeal(
            TestUnit healer, TestUnit target, ref WeaponData staff,
            out int amountHealed, out string failReason)
        {
            amountHealed = 0;
            if (!CanUseStaff(healer, staff, out failReason)) return false;
            if (!CanHealTarget(healer, target, staff, GetMag(healer), out failReason)) return false;

            GetHP(target, out int currentHP, out int maxHP);
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
            if (!CanUseStaff(healer, staff, out failReason)) return false;

            int mag = GetMag(healer);

            foreach (var unit in allUnits)
            {
                if (!IsValidFortifyTarget(healer, unit, staff, mag)) continue;

                GetHP(unit, out int currentHP, out int maxHP);
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

        // TODO: USE-01 — when rescue system exists, allow targeting carrier's tile to heal carried unit.

        // --- Private helpers ---

        private static bool IsValidFortifyTarget(TestUnit healer, TestUnit unit, WeaponData staff, int mag)
        {
            if (unit == null) return false;
            if (unit.faction != healer.faction && unit.faction != Faction.Allied) return false;
            if (!StaffRangeResolver.IsInRange(staff, mag, healer.gridPosition, unit.gridPosition)) return false;

            GetHP(unit, out int currentHP, out int maxHP);
            return currentHP < maxHP;
        }

        private static void GrantHealExp(TestUnit healer, TestUnit healed)
        {
            if (ExpGranter.Instance == null) return;
            if (healer == null || healed == null) return;
            if (healer.UnitInstance == null || healed.UnitInstance == null) return;

            int exp = ExpMath.ComputeHealExp(healer.UnitInstance, healed.UnitInstance);
            ExpGranter.Instance.Grant(healer, exp);
        }

        private static int GetMag(TestUnit unit) =>
            unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;

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
    }
}
