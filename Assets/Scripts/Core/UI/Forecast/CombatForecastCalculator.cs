using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Forecast
{
    // Pure service: turns two units into a Combat Forecast presentation model. Dispatches on
    // the forecast kind, then owns the joint forecast math — weapon triangle + effectiveness
    // layered on CombatForecast.Compute — and the HP-based portrait choice. No Unity lifecycle.
    public sealed class CombatForecastCalculator
    {
        public CombatForecastModel Build(ForecastKind kind, TestUnit actor, TestUnit target)
        {
            switch (kind)
            {
                case ForecastKind.StaffHeal:      return BuildStaffHeal(actor, target);
                case ForecastKind.StaffOffensive: return BuildStaffOffensive(actor, target);
                default:                          return BuildCombat(actor, target);
            }
        }

        private CombatForecastModel BuildCombat(TestUnit attacker, TestUnit defender)
        {
            int distance = Mathf.Abs(attacker.gridPosition.x - defender.gridPosition.x)
                         + Mathf.Abs(attacker.gridPosition.y - defender.gridPosition.y);

            CombatForecast forecast = BuildForecast(attacker, defender, distance, out int triangle, out bool attackerEffective, out bool defenderEffective);

            return new CombatForecastModel
            {
                TriangleAdvantage = ToAdvantage(triangle),
                AttackerEffective = attackerEffective,
                DefenderEffective = defenderEffective,
                AttackerSideModel = BuildSide(attacker, forecast.AttackerDamage, forecast.AttackerHit, forecast.AttackerCritRate, forecast.AttackerCanDouble, true, true),
                DefenderSideModel = BuildSide(defender, forecast.DefenderDamage, forecast.DefenderHit, forecast.DefenderCritRate, forecast.DefenderCanDouble, forecast.DefenderCanCounter, false),
            };
        }

        // Heal preview — the healer shows the heal amount in the Atk slot.
        private CombatForecastModel BuildStaffHeal(TestUnit healer, TestUnit target)
        {
            var staff = healer.Inventory.GetEquippedWeapon();
            int mag = healer.UnitInstance != null ? healer.UnitInstance.Stats[StatIndex.Mag] : 0;
            int curHp = target.UnitInstance?.CurrentHP ?? target.currentHP;
            int maxHp = target.UnitInstance?.MaxHP ?? target.maxHP;
            int heal = StaffEffects.ComputeHealAmount(staff, mag, curHp, maxHp);

            return new CombatForecastModel
            {
                AttackerSideModel = BuildSide(healer, heal, 100, 0, false, true, true),
                DefenderSideModel = BuildSide(target, 0, 0, 0, false, false, false),
            };
        }

        // Offensive-staff preview — the caster shows a hit% in the Hit slot.
        private CombatForecastModel BuildStaffOffensive(TestUnit caster, TestUnit target)
        {
            int mag = caster.UnitInstance?.Stats[StatIndex.Mag] ?? 0;
            int skl = caster.UnitInstance?.Stats[StatIndex.Skl] ?? 0;
            int res = target.UnitInstance?.Stats[StatIndex.Res] ?? 0;
            int hit = StaffEffects.ComputeStaffHit(mag, skl, res);

            return new CombatForecastModel
            {
                AttackerSideModel = BuildSide(caster, 0, hit, 0, false, true, true),
                DefenderSideModel = BuildSide(target, 0, 0, 0, false, false, false),
            };
        }

        private static SideModel BuildSide(TestUnit unit, int atk, int hit, int crit, bool canDouble, bool showOffense, bool flipPortrait)
        {
            var weapon = unit.Inventory.GetEquippedWeapon();
            return new SideModel
            {
                UnitName = unit.UnitInstance?.Definition?.UnitName ?? unit.name,
                Portrait = PickPortrait(unit),
                FlipPortrait = flipPortrait,
                Faction = unit.faction,
                WeaponName = weapon.IsEmpty ? "—" : weapon.name,
                WeaponType = weapon.weaponType,
                HasWeapon = !weapon.IsEmpty,
                Atk = atk,
                Hit = hit,
                Crit = crit,
                CurrentHP = unit.UnitInstance?.CurrentHP ?? unit.currentHP,
                MaxHP = unit.UnitInstance?.MaxHP ?? unit.maxHP,
                CanDouble = canDouble,
                ShowOffense = showOffense,
            };
        }

        // The HP-appropriate portrait variant; null until portraits are authored.
        private static Sprite PickPortrait(TestUnit unit)
        {
            var def = unit.UnitInstance?.Definition;
            if (def == null) return null;
            int cur = unit.UnitInstance?.CurrentHP ?? unit.currentHP;
            int max = unit.UnitInstance?.MaxHP ?? unit.maxHP;
            float frac = max > 0 ? (float)cur / max : 1f;
            if (cur <= 0 && def.DeceasedPortrait != null) return def.DeceasedPortrait;
            if (frac < 0.25f && def.CriticalPortrait != null) return def.CriticalPortrait;
            if (frac < 0.5f && def.WoundedPortrait != null) return def.WoundedPortrait;
            return def.Portrait;
        }

        private static CombatForecast BuildForecast(TestUnit attacker, TestUnit defender, int distance, out int weaponTriangle, out bool attackerEffective, out bool defenderEffective)
        {
            WeaponData atkWeapon = attacker.Inventory.GetEquippedWeapon();
            WeaponData defWeapon = defender.Inventory.GetEquippedWeapon();

            ClassType defClass = defender.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            ClassType atkClass = attacker.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            attackerEffective = !atkWeapon.IsEmpty && atkWeapon.IsEffectiveAgainst(defClass);
            defenderEffective = !defWeapon.IsEmpty && defWeapon.IsEffectiveAgainst(atkClass);

            weaponTriangle = atkWeapon.IsEmpty || defWeapon.IsEmpty
                ? 0
                : WeaponTriangle.ComputeAdvantage(atkWeapon, defWeapon);

            StatArray atkStats = attacker.UnitInstance != null ? attacker.UnitInstance.Stats : StatArray.From(20, 10, 8, 8, 8, 8, 8, 5, 5);
            StatArray defStats = defender.UnitInstance != null ? defender.UnitInstance.Stats : StatArray.From(20, 10, 8, 8, 8, 8, 8, 5, 5);

            CombatantData atkData = CombatantData.FromStats(atkStats, attacker.UnitInstance?.CurrentHP ?? attacker.currentHP, attacker.UnitInstance?.MaxHP ?? attacker.maxHP, EffectiveWeapon(atkWeapon, attackerEffective), distance);
            CombatantData defData = CombatantData.FromStats(defStats, defender.UnitInstance?.CurrentHP ?? defender.currentHP, defender.UnitInstance?.MaxHP ?? defender.maxHP, EffectiveWeapon(defWeapon, defenderEffective), distance);

            CombatForecast combatForecast = CombatForecast.Compute(atkData, defData, 0, 0, 0, 0);

            if (weaponTriangle == 1)
            {
                combatForecast.AttackerHit = Mathf.Clamp(combatForecast.AttackerHit + 15, 0, 100);
                combatForecast.AttackerDamage = Mathf.Max(0, combatForecast.AttackerDamage + 1);
                combatForecast.DefenderHit = Mathf.Clamp(combatForecast.DefenderHit - 15, 0, 100);
                combatForecast.DefenderDamage = Mathf.Max(0, combatForecast.DefenderDamage - 1);
            }
            else if (weaponTriangle == -1)
            {
                combatForecast.AttackerHit = Mathf.Clamp(combatForecast.AttackerHit - 15, 0, 100);
                combatForecast.AttackerDamage = Mathf.Max(0, combatForecast.AttackerDamage - 1);
                combatForecast.DefenderHit = Mathf.Clamp(combatForecast.DefenderHit + 15, 0, 100);
                combatForecast.DefenderDamage = Mathf.Max(0, combatForecast.DefenderDamage + 1);
            }
            return combatForecast;
        }

        private static TriangleAdvantage ToAdvantage(int sign) =>
            sign > 0 ? TriangleAdvantage.Attacker
          : sign < 0 ? TriangleAdvantage.Defender
          : TriangleAdvantage.None;

        private static WeaponData EffectiveWeapon(WeaponData w, bool effective)
        {
            if (!effective || w.IsEmpty) return w;
            var copy = w;
            copy.might = w.might * 3;
            return copy;
        }
    }
}
