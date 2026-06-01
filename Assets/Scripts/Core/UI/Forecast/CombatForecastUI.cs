using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Forecast
{
    // Combat Forecast Panel — previews the outcome of a player-chosen attack/heal.
    // Read-only: all numbers come from CombatForecast.Compute (adjusted for weapon
    // triangle + effectiveness) and are pushed into the prefab via CombatForecastRefs.
    public class CombatForecastUI : MonoBehaviour
    {
        public static bool IsVisible { get; private set; }

        [SerializeField] private GameObject _popupInstance;

        private CombatForecastRefs _refs;

        public void ShowCombat(TestUnit attacker, TestUnit defender, int distance)
        {
            if (!ActivateUI()) return;
            if (attacker == null || defender == null) { Hide(); return; }

            var forecast = BuildForecast(attacker, defender, distance, out _, out _, out _);

            DriveSide(_refs.left, attacker, forecast.AttackerDamage, forecast.AttackerHit, forecast.AttackerCritRate, true, true);
            DriveSide(_refs.right, defender, forecast.DefenderDamage, forecast.DefenderHit, forecast.DefenderCritRate,
                forecast.DefenderCanCounter, false);

            IsVisible = true;
        }

        public void Hide()
        {
            if (_popupInstance != null) _popupInstance.SetActive(false);
            IsVisible = false;
        }

        // Heal preview — left = healer (heal amount in the Atk slot), right = target.
        public void ShowStaffHeal(TestUnit healer, TestUnit target)
        {
            if (!ActivateUI()) return;
            if (healer == null || target == null) { Hide(); return; }

            var staff = healer.Inventory.GetEquippedWeapon();
            int mag = healer.UnitInstance != null ? healer.UnitInstance.Stats[StatIndex.Mag] : 0;
            int curHp = target.UnitInstance?.CurrentHP ?? target.currentHP;
            int maxHp = target.UnitInstance?.MaxHP ?? target.maxHP;
            int heal = StaffEffects.ComputeHealAmount(staff, mag, curHp, maxHp);

            DriveSide(_refs.left, healer, heal, 100, 0, true, true);
            DriveSide(_refs.right, target, 0, 0, 0, false, false);

            IsVisible = true;
        }

        // Offensive-staff preview — left = caster (hit% in the Hit slot), right = target.
        public void ShowStaffOffensive(TestUnit caster, TestUnit target)
        {
            if (!ActivateUI()) return;
            if (caster == null || target == null) { Hide(); return; }

            int mag = caster.UnitInstance?.Stats[StatIndex.Mag] ?? 0;
            int skl = caster.UnitInstance?.Stats[StatIndex.Skl] ?? 0;
            int res = target.UnitInstance?.Stats[StatIndex.Res] ?? 0;
            int hit = StaffEffects.ComputeStaffHit(mag, skl, res);

            DriveSide(_refs.left, caster, 0, hit, 0, true, true);
            DriveSide(_refs.right, target, 0, 0, 0, false, false);

            IsVisible = true;
        }

        void OnDestroy() { if (IsVisible) Hide(); }

        // ==================================================================
        // Binding — pushes one side's data into the prefab refs.
        // ==================================================================

        private void DriveSide(CombatForecastRefs.UnitSide side, TestUnit unit,
            int damage, int hit, int crit, bool showOffense, bool flipPortrait)
        {
            if (side == null || unit == null) return;

            if (side.unitName != null) side.unitName.text = unit.UnitInstance?.Definition?.UnitName ?? unit.name;
            if (side.portrait != null)
            {
                var p = PickPortrait(unit);
                if (p != null) side.portrait.sprite = p;
                // Portraits face left by default; the left side looks right (toward the foe), the right side looks left.
                var prt = side.portrait.rectTransform;
                float mag = Mathf.Abs(prt.localScale.x) < 0.0001f ? 1f : Mathf.Abs(prt.localScale.x);
                var ls = prt.localScale; ls.x = flipPortrait ? -mag : mag; prt.localScale = ls;
            }

            var weapon = unit.Inventory.GetEquippedWeapon();
            if (side.weaponName != null) side.weaponName.text = weapon.IsEmpty ? "—" : weapon.name;
            if (side.weaponIcon != null) side.weaponIcon.sprite = SigilFor(weapon);

            int curHp = unit.UnitInstance?.CurrentHP ?? unit.currentHP;
            if (side.hpValue != null) side.hpValue.text = curHp.ToString();

            if (side.atkValue != null)  side.atkValue.text  = showOffense ? damage.ToString() : "—";
            if (side.hitValue != null)  side.hitValue.text  = showOffense ? hit + "%" : "—";
            if (side.critValue != null) side.critValue.text = showOffense ? crit + "%" : "—";
        }

        // Picks the HP-appropriate portrait variant from the unit's definition; null
        // until portraits are authored (the Image keeps its placeholder meanwhile).
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

        private Sprite SigilFor(WeaponData w)
        {
            if (w.IsEmpty) return null;
            switch (w.weaponType)
            {
                case WeaponType.Sword:     return _refs.sigilSword;
                case WeaponType.Lance:     return _refs.sigilLance;
                case WeaponType.Axe:       return _refs.sigilAxe;
                case WeaponType.Bow:       return _refs.sigilBow;
                case WeaponType.Staff:     return _refs.sigilStaff;
                case WeaponType.AnimaTome: return _refs.sigilAnima;
                case WeaponType.LightTome: return _refs.sigilLight;
                case WeaponType.DarkTome:  return _refs.sigilDark;
                default:                   return _refs.sigilSword;
            }
        }

        // ==================================================================
        // Forecast math (weapon triangle + effectiveness on top of Compute).
        // ==================================================================

        private CombatForecast BuildForecast(TestUnit attacker, TestUnit defender, int distance,
            out int weaponTriangle, out bool attackerEffective, out bool defenderEffective)
        {
            weaponTriangle = 0;
            attackerEffective = false;
            defenderEffective = false;

            var atkWeapon = attacker.Inventory.GetEquippedWeapon();
            var defWeapon = defender.Inventory.GetEquippedWeapon();

            var defClass = defender.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            var atkClass = attacker.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            attackerEffective = !atkWeapon.IsEmpty && atkWeapon.IsEffectiveAgainst(defClass);
            defenderEffective = !defWeapon.IsEmpty && defWeapon.IsEffectiveAgainst(atkClass);

            weaponTriangle = atkWeapon.IsEmpty || defWeapon.IsEmpty
                ? 0
                : WeaponTriangle.ComputeAdvantage(atkWeapon, defWeapon);

            var atkStats = attacker.UnitInstance != null ? attacker.UnitInstance.Stats : StatArray.From(20, 10, 8, 8, 8, 8, 8, 5, 5);
            var defStats = defender.UnitInstance != null ? defender.UnitInstance.Stats : StatArray.From(20, 10, 8, 8, 8, 8, 8, 5, 5);

            var atkData = CombatantData.FromStats(atkStats,
                attacker.UnitInstance?.CurrentHP ?? attacker.currentHP,
                attacker.UnitInstance?.MaxHP ?? attacker.maxHP,
                EffectiveWeapon(atkWeapon, attackerEffective), distance);
            var defData = CombatantData.FromStats(defStats,
                defender.UnitInstance?.CurrentHP ?? defender.currentHP,
                defender.UnitInstance?.MaxHP ?? defender.maxHP,
                EffectiveWeapon(defWeapon, defenderEffective), distance);

            int defTerrainDef = 0, defTerrainAvo = 0, atkTerrainDef = 0, atkTerrainAvo = 0;
            var f = CombatForecast.Compute(atkData, defData, defTerrainDef, defTerrainAvo, atkTerrainDef, atkTerrainAvo);

            if (weaponTriangle == 1)
            {
                f.AttackerHit = Mathf.Clamp(f.AttackerHit + 15, 0, 100);
                f.AttackerDamage = Mathf.Max(0, f.AttackerDamage + 1);
                f.DefenderHit = Mathf.Clamp(f.DefenderHit - 15, 0, 100);
                f.DefenderDamage = Mathf.Max(0, f.DefenderDamage - 1);
            }
            else if (weaponTriangle == -1)
            {
                f.AttackerHit = Mathf.Clamp(f.AttackerHit - 15, 0, 100);
                f.AttackerDamage = Mathf.Max(0, f.AttackerDamage - 1);
                f.DefenderHit = Mathf.Clamp(f.DefenderHit + 15, 0, 100);
                f.DefenderDamage = Mathf.Max(0, f.DefenderDamage + 1);
            }
            return f;
        }

        private static WeaponData EffectiveWeapon(WeaponData w, bool effective)
        {
            if (!effective || w.IsEmpty) return w;
            var copy = w;
            copy.might = w.might * 3;
            return copy;
        }

        // ==================================================================
        // Activation + positioning
        // ==================================================================

        private bool ActivateUI()
        {
            if (_popupInstance == null)
            {
                Debug.LogError("CombatForecastUI: _popupInstance not wired. Run scene setup or rebuild the prefab.");
                return false;
            }
            if (_refs == null) _refs = _popupInstance.GetComponent<CombatForecastRefs>();
            if (_refs == null)
            {
                Debug.LogError("CombatForecastUI: prefab missing CombatForecastRefs.");
                return false;
            }

            _popupInstance.SetActive(true);
            _popupInstance.transform.SetAsLastSibling();
            return true;
        }
    }
}
