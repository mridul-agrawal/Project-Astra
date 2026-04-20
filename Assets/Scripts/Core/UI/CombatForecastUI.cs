using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Combat Forecast Panel — previews the outcome of a player-chosen attack before commit.
    /// Drives the CombatForecast.prefab via CombatForecastRefs. Position flips between
    /// top-right and top-left based on the cursor's map-space position so the panel never
    /// covers the target tile.
    ///
    /// Game logic is read-only: no mutation of units, weapons, or convoy. All numbers come
    /// from CombatForecast.Compute (pure, in Core/Map), adjusted for weapon triangle and
    /// effectiveness before being pushed into the UI.
    /// </summary>
    public class CombatForecastUI : MonoBehaviour
    {
        public static bool IsVisible { get; private set; }

        [SerializeField] private GameObject _popupInstance;

        // HP bar colour thresholds — mirrors UnitInfoPanelUI.
        static readonly Color HpGreen  = new Color32(0x60, 0xc8, 0x70, 0xff);
        static readonly Color HpYellow = new Color32(0xc8, 0xb0, 0x40, 0xff);
        static readonly Color HpRed    = new Color32(0xc8, 0x40, 0x40, 0xff);

        private CombatForecastRefs _refs;
        private RectTransform _panelRt;

        public void ShowCombat(TestUnit attacker, TestUnit defender, int distance)
        {
            if (!ActivateUI()) return;
            if (attacker == null || defender == null) { Hide(); return; }

            var forecast = BuildForecast(attacker, defender, distance, out var weaponTriangle, out var atkEffective, out var defEffective);

            DriveSide(_refs.left,  attacker, forecast.AttackerDamage, forecast.AttackerHit, forecast.AttackerCritRate,
                attackerBase: true,
                canDouble: forecast.AttackerCanDouble,
                brave: !attacker.Inventory.GetEquippedWeapon().IsEmpty && attacker.Inventory.GetEquippedWeapon().brave,
                predictedDamage: forecast.AttackerDamage * TotalHits(forecast.AttackerCanDouble, attacker.Inventory.GetEquippedWeapon().brave),
                incomingDamage: forecast.DefenderCanCounter
                    ? forecast.DefenderDamage * TotalHits(forecast.DefenderCanDouble, defender.Inventory.GetEquippedWeapon().brave)
                    : 0,
                weaponArrow: weaponTriangle == 1 ? "▲" : (weaponTriangle == -1 ? "▼" : ""),
                effective: atkEffective);

            DriveSide(_refs.right, defender, forecast.DefenderDamage, forecast.DefenderHit, forecast.DefenderCritRate,
                attackerBase: false,
                canDouble: forecast.DefenderCanDouble,
                brave: !defender.Inventory.GetEquippedWeapon().IsEmpty && defender.Inventory.GetEquippedWeapon().brave,
                predictedDamage: forecast.DefenderCanCounter
                    ? forecast.DefenderDamage * TotalHits(forecast.DefenderCanDouble, defender.Inventory.GetEquippedWeapon().brave)
                    : 0,
                incomingDamage: forecast.AttackerDamage * TotalHits(forecast.AttackerCanDouble, attacker.Inventory.GetEquippedWeapon().brave),
                weaponArrow: weaponTriangle == -1 ? "▲" : (weaponTriangle == 1 ? "▼" : ""),
                effective: defEffective);

            // DMG hero numbers (spine)
            _refs.attackerDmgNum.text = forecast.AttackerDamage.ToString();
            _refs.defenderDmgNum.text = forecast.DefenderCanCounter ? forecast.DefenderDamage.ToString() : "—";
            if (_refs.defenderDmgNum != null)
                _refs.defenderDmgNum.color = forecast.DefenderCanCounter
                    ? new Color32(0xff, 0xcd, 0xb8, 0xff) : new Color32(0xc9, 0xb9, 0x8a, 0xff);

            // Stat rows
            _refs.hitAttackerVal.text = forecast.AttackerHit + "%";
            _refs.hitDefenderVal.text = forecast.DefenderCanCounter ? forecast.DefenderHit + "%" : "—";
            _refs.critAttackerVal.text = forecast.AttackerCritRate + "%";
            _refs.critDefenderVal.text = forecast.DefenderCanCounter ? forecast.DefenderCritRate + "%" : "—";

            // No-counter ribbon
            if (_refs.noCounterRibbon != null)
            {
                if (!forecast.DefenderCanCounter)
                {
                    _refs.noCounterRibbon.SetActive(true);
                    _refs.noCounterLabel.text = OutOfRangeReason(attacker, defender, distance);
                }
                else _refs.noCounterRibbon.SetActive(false);
            }

            // Flip L/R based on cursor screen-space position
            RepositionPanel(defender);
            IsVisible = true;
        }

        public void Hide()
        {
            if (_popupInstance != null) _popupInstance.SetActive(false);
            IsVisible = false;
        }

        // Heal preview — simplified overlay: reuses the main panel with heal-specific messaging.
        // For MVP we reuse the forecast panel and show heal amount in the attacker DMG slot,
        // suppress the defender DMG/HIT/CRIT, and display "CANNOT COUNTER" treatment on the
        // right side.
        public void ShowStaffHeal(TestUnit healer, TestUnit target)
        {
            if (!ActivateUI()) return;
            if (healer == null || target == null) { Hide(); return; }

            var staff = healer.Inventory.GetEquippedWeapon();
            int mag = healer.UnitInstance != null ? healer.UnitInstance.Stats[StatIndex.Mag] : 0;
            int curHp = target.UnitInstance?.CurrentHP ?? target.currentHP;
            int maxHp = target.UnitInstance?.MaxHP ?? target.maxHP;
            int healAmount = StaffEffects.ComputeHealAmount(staff, mag, curHp, maxHp);

            DriveSideForHeal(_refs.left,  healer, "HEALS", healAmount, incomingDamage: 0, predictedDelta: 0);
            DriveSideForHeal(_refs.right, target, "RECEIVES", healAmount, incomingDamage: 0, predictedDelta: healAmount);

            _refs.attackerDmgNum.text = "+" + healAmount;
            _refs.defenderDmgNum.text = "—";
            _refs.hitAttackerVal.text = "—";
            _refs.hitDefenderVal.text = "—";
            _refs.critAttackerVal.text = "—";
            _refs.critDefenderVal.text = "—";

            if (_refs.noCounterRibbon != null) _refs.noCounterRibbon.SetActive(false);

            RepositionPanel(target);
            IsVisible = true;
        }

        public void ShowStaffOffensive(TestUnit caster, TestUnit target)
        {
            if (!ActivateUI()) return;
            if (caster == null || target == null) { Hide(); return; }

            int mag = caster.UnitInstance?.Stats[StatIndex.Mag] ?? 0;
            int skl = caster.UnitInstance?.Stats[StatIndex.Skl] ?? 0;
            int tgtRes = target.UnitInstance?.Stats[StatIndex.Res] ?? 0;
            int hit = StaffEffects.ComputeStaffHit(mag, skl, tgtRes);

            DriveSideForHeal(_refs.left,  caster, "CAST", 0, incomingDamage: 0, predictedDelta: 0);
            DriveSideForHeal(_refs.right, target, "TARGET", 0, incomingDamage: 0, predictedDelta: 0);

            _refs.attackerDmgNum.text = hit + "%";
            _refs.defenderDmgNum.text = "—";
            _refs.hitAttackerVal.text = hit + "%";
            _refs.hitDefenderVal.text = "—";
            _refs.critAttackerVal.text = "—";
            _refs.critDefenderVal.text = "—";

            if (_refs.noCounterRibbon != null) _refs.noCounterRibbon.SetActive(false);
            RepositionPanel(target);
            IsVisible = true;
        }

        void OnDestroy() { if (IsVisible) Hide(); }

        // ==================================================================
        // Binding
        // ==================================================================

        private void DriveSide(CombatForecastRefs.UnitSide side, TestUnit unit,
            int damage, int hit, int crit, bool attackerBase,
            bool canDouble, bool brave, int predictedDamage, int incomingDamage,
            string weaponArrow, bool effective)
        {
            if (side == null) return;
            side.unitName.text = unit.name;
            side.unitSub.text = ClassLabel(unit);
            side.levelValue.text = (unit.UnitInstance?.Level ?? 0).ToString();
            side.classValue.text = unit.UnitInstance?.CurrentClass?.ClassName ?? "—";

            var weapon = unit.Inventory.GetEquippedWeapon();
            side.weaponName.text = weapon.IsEmpty ? "—" : weapon.name;
            if (side.weaponIcon != null) side.weaponIcon.sprite = SigilFor(weapon);
            side.weaponArrow.text = weaponArrow;
            side.weaponArrow.color = weaponArrow == "▲" ? HpGreen : (weaponArrow == "▼" ? new Color32(0xd1, 0x4a, 0x34, 0xff) : Color.clear);

            int curHp = unit.UnitInstance?.CurrentHP ?? unit.currentHP;
            int maxHp = unit.UnitInstance?.MaxHP ?? unit.maxHP;
            int predHp = Mathf.Max(0, curHp - incomingDamage);
            bool predKo = incomingDamage >= curHp && curHp > 0;

            side.hpNumeric.text = $"{curHp} / {maxHp}";
            if (side.hpDelta != null)
                side.hpDelta.text = incomingDamage > 0 ? $"−{incomingDamage}" : "";

            // HP bar widths
            float trackW = side.hpTrackBg != null ? side.hpTrackBg.rectTransform.rect.width : 0;
            if (trackW > 0 && maxHp > 0)
            {
                float curFrac = Mathf.Clamp01((float)curHp / maxHp);
                float predFrac = Mathf.Clamp01((float)predHp / maxHp);

                if (side.hpFill != null)
                {
                    var r = side.hpFill.rectTransform;
                    r.sizeDelta = new Vector2(trackW * curFrac, r.sizeDelta.y);
                    side.hpFill.sprite = HpFillFor(curHp, maxHp);
                    side.hpFill.gameObject.SetActive(curFrac > 0.001f);
                }
                if (side.hpPredOverlay != null)
                {
                    var r = side.hpPredOverlay.rectTransform;
                    float left  = trackW * predFrac;
                    float width = trackW * (curFrac - predFrac);
                    r.anchoredPosition = new Vector2(left, r.anchoredPosition.y);
                    r.sizeDelta = new Vector2(Mathf.Max(0, width), r.sizeDelta.y);
                    side.hpPredOverlay.gameObject.SetActive(width > 0.5f && !predKo);
                }
            }

            if (side.koBadgeTransform != null)
                side.koBadgeTransform.gameObject.SetActive(predKo);

            if (side.effectiveChip != null) side.effectiveChip.SetActive(effective);

            // Doubling chip
            bool showChip = canDouble || brave;
            if (side.doubleChipRoot != null) side.doubleChipRoot.SetActive(showChip);
            if (showChip)
            {
                if (canDouble && brave)
                {
                    side.doubleChipNumber.text = "×4";
                    side.doubleChipTag.text = "COMBINED";
                    if (side.doubleChipBg != null) side.doubleChipBg.sprite = _refs.chipDoubleCombined;
                }
                else if (brave)
                {
                    side.doubleChipNumber.text = "×2";
                    side.doubleChipTag.text = "BRAVE";
                    if (side.doubleChipBg != null) side.doubleChipBg.sprite = _refs.chipDoubleBrave;
                }
                else
                {
                    side.doubleChipNumber.text = "×2";
                    side.doubleChipTag.text = "AS DOUBLE";
                    if (side.doubleChipBg != null) side.doubleChipBg.sprite = _refs.chipDoubleAs;
                }
            }
        }

        private void DriveSideForHeal(CombatForecastRefs.UnitSide side, TestUnit unit,
            string weaponArrow, int amount, int incomingDamage, int predictedDelta)
        {
            if (side == null) return;
            side.unitName.text = unit.name;
            side.unitSub.text = ClassLabel(unit);
            side.levelValue.text = (unit.UnitInstance?.Level ?? 0).ToString();
            side.classValue.text = unit.UnitInstance?.CurrentClass?.ClassName ?? "—";

            var weapon = unit.Inventory.GetEquippedWeapon();
            side.weaponName.text = weapon.IsEmpty ? "—" : weapon.name;
            if (side.weaponIcon != null) side.weaponIcon.sprite = SigilFor(weapon);
            side.weaponArrow.text = "";
            side.weaponArrow.color = Color.clear;

            int curHp = unit.UnitInstance?.CurrentHP ?? unit.currentHP;
            int maxHp = unit.UnitInstance?.MaxHP ?? unit.maxHP;
            int predHp = Mathf.Clamp(curHp + predictedDelta - incomingDamage, 0, maxHp);

            side.hpNumeric.text = $"{curHp} / {maxHp}";
            if (side.hpDelta != null)
                side.hpDelta.text = predictedDelta > 0 ? $"+{predictedDelta}" : (incomingDamage > 0 ? $"−{incomingDamage}" : "");
            if (side.hpDelta != null)
                side.hpDelta.color = predictedDelta > 0 ? HpGreen : new Color32(0xd1, 0x4a, 0x34, 0xff);

            float trackW = side.hpTrackBg != null ? side.hpTrackBg.rectTransform.rect.width : 0;
            if (trackW > 0 && maxHp > 0)
            {
                float curFrac = Mathf.Clamp01((float)curHp / maxHp);
                float predFrac = Mathf.Clamp01((float)predHp / maxHp);

                if (side.hpFill != null)
                {
                    var r = side.hpFill.rectTransform;
                    r.sizeDelta = new Vector2(trackW * curFrac, r.sizeDelta.y);
                    side.hpFill.sprite = HpFillFor(curHp, maxHp);
                    side.hpFill.gameObject.SetActive(curFrac > 0.001f);
                }
                if (side.hpPredOverlay != null)
                {
                    var r = side.hpPredOverlay.rectTransform;
                    float left  = trackW * Mathf.Min(curFrac, predFrac);
                    float width = trackW * Mathf.Abs(curFrac - predFrac);
                    r.anchoredPosition = new Vector2(left, r.anchoredPosition.y);
                    r.sizeDelta = new Vector2(Mathf.Max(0, width), r.sizeDelta.y);
                    side.hpPredOverlay.gameObject.SetActive(width > 0.5f);
                }
            }

            if (side.koBadgeTransform != null) side.koBadgeTransform.gameObject.SetActive(false);
            if (side.effectiveChip != null) side.effectiveChip.SetActive(false);
            if (side.doubleChipRoot != null) side.doubleChipRoot.SetActive(false);
        }

        private Sprite HpFillFor(int cur, int max)
        {
            if (max <= 0) return _refs.hpFillRed;
            int pct = cur * 100 / max;
            if (pct > 50) return _refs.hpFillGreen;
            if (pct >= 31) return _refs.hpFillYellow;
            return _refs.hpFillRed;
        }

        private Sprite SigilFor(WeaponData w)
        {
            if (w.IsEmpty) return null;
            return w.weaponType switch
            {
                WeaponType.Sword     => _refs.sigilSword,
                WeaponType.Lance     => _refs.sigilLance,
                WeaponType.Axe       => _refs.sigilAxe,
                WeaponType.Bow       => _refs.sigilBow,
                WeaponType.Staff     => _refs.sigilStaff,
                WeaponType.AnimaTome => _refs.sigilAnima,
                WeaponType.LightTome => _refs.sigilLight,
                WeaponType.DarkTome  => _refs.sigilDark,
                _                    => _refs.sigilSword,
            };
        }

        private static string ClassLabel(TestUnit unit)
        {
            if (unit == null) return "—";
            var def = unit.UnitInstance?.Definition;
            var cls = unit.UnitInstance?.CurrentClass;
            string a = def?.UnitName ?? unit.name;
            string b = cls?.ClassName ?? "—";
            return $"{b} · {a}";
        }

        private static int TotalHits(bool canDouble, bool brave)
        {
            int n = 1;
            if (brave) n *= 2;
            if (canDouble) n *= 2;
            return n;
        }

        // Weapon triangle + effectiveness adjustments on top of the base CombatForecast.Compute.
        // weaponTriangle: +1 attacker advantage, -1 disadvantage, 0 neutral.
        private CombatForecast BuildForecast(TestUnit attacker, TestUnit defender, int distance,
            out int weaponTriangle, out bool attackerEffective, out bool defenderEffective)
        {
            weaponTriangle = 0;
            attackerEffective = false;
            defenderEffective = false;

            var atkWeapon = attacker.Inventory.GetEquippedWeapon();
            var defWeapon = defender.Inventory.GetEquippedWeapon();

            // Effectiveness check — use defender's class if available, otherwise fall through.
            var defClass = defender.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            var atkClass = attacker.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            attackerEffective = !atkWeapon.IsEmpty && atkWeapon.IsEffectiveAgainst(defClass);
            defenderEffective = !defWeapon.IsEmpty && defWeapon.IsEffectiveAgainst(atkClass);

            // Weapon triangle advantage (returns +1 / 0 / −1)
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

            // Terrain bonuses — zero today until TerrainLookup is wired.
            int defTerrainDef = 0, defTerrainAvo = 0, atkTerrainDef = 0, atkTerrainAvo = 0;

            var f = CombatForecast.Compute(atkData, defData, defTerrainDef, defTerrainAvo, atkTerrainDef, atkTerrainAvo);

            // Apply weapon-triangle bonuses: +15 hit, +1 damage for advantaged side; mirror for disadvantaged.
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

        // Returns a weapon with might = might × 3 when the effectiveness flag is true, so the
        // damage formula includes the tripled might. Everything else is unchanged.
        private static WeaponData EffectiveWeapon(WeaponData w, bool effective)
        {
            if (!effective || w.IsEmpty) return w;
            var copy = w;
            copy.might = w.might * 3;
            return copy;
        }

        private static string OutOfRangeReason(TestUnit attacker, TestUnit defender, int distance)
        {
            var def = defender.Inventory.GetEquippedWeapon();
            if (def.IsEmpty) return "CANNOT COUNTER";
            if (!def.CanReachRange(distance)) return "OUT OF RANGE";
            return "CANNOT COUNTER";
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
            if (_panelRt == null)
            {
                var panel = _popupInstance.transform.Find("Panel") as RectTransform;
                _panelRt = panel;
            }
            _popupInstance.SetActive(true);
            _popupInstance.transform.SetAsLastSibling();
            return true;
        }

        // Flip panel to the opposite side of the cursor/target so the tile stays visible.
        private void RepositionPanel(TestUnit target)
        {
            if (_panelRt == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var worldPos = new Vector3(target.transform.position.x, target.transform.position.y, 0);
            var screenPos = cam.WorldToScreenPoint(worldPos);
            bool cursorOnRight = screenPos.x > Screen.width * 0.5f;

            if (cursorOnRight)
            {
                _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0f, 1f);
                _panelRt.pivot = new Vector2(0f, 1f);
                _panelRt.anchoredPosition = new Vector2(40f, -40f);
            }
            else
            {
                _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(1f, 1f);
                _panelRt.pivot = new Vector2(1f, 1f);
                _panelRt.anchoredPosition = new Vector2(-40f, -40f);
            }
        }
    }
}
