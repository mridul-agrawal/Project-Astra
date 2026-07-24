using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Forecast
{
    // Renders one combatant's side of the Combat Forecast — the same component is reused
    // for attacker and defender. Pure presentation: pushes a SideModel onto its widgets.
    public sealed class CombatForecastSideView : MonoBehaviour
    {
        [Header("Widgets")]
        public Image NameBanner;
        public TextMeshProUGUI UnitName;
        public Image Portrait;
        public Image HpBar;
        public TextMeshProUGUI HpValue;
        public Image WeaponBanner;
        public TextMeshProUGUI WeaponName;
        public Image WeaponIcon;
        public TextMeshProUGUI AtkValue;
        public TextMeshProUGUI HitValue;
        public TextMeshProUGUI CritValue;
        public GameObject DoubleBadge;   // "x2" — shown when this side doubles

        [Header("Weapon sigils (by weapon type)")]
        public Sprite SigilSword;
        public Sprite SigilLance;
        public Sprite SigilAxe;
        public Sprite SigilBow;
        public Sprite SigilStaff;
        public Sprite SigilAnima;
        public Sprite SigilLight;
        public Sprite SigilDark;

        [Header("Faction banner tints")]
        public Color PlayerTint = new Color(0.29f, 0.45f, 0.78f);
        public Color EnemyTint  = new Color(0.72f, 0.24f, 0.24f);
        public Color AlliedTint = new Color(0.35f, 0.62f, 0.40f);

        public void Render(SideModel data)
        {
            if (data == null) return;

            if (UnitName != null)   UnitName.text = data.UnitName;
            if (NameBanner != null) NameBanner.color = TintFor(data.Faction);
            RenderPortrait(data);

            if (WeaponName != null) WeaponName.text = data.WeaponName;
            if (WeaponIcon != null) WeaponIcon.sprite = SigilFor(data.WeaponType, data.HasWeapon);

            if (HpValue != null) HpValue.text = data.CurrentHP.ToString();
            if (HpBar != null)   HpBar.fillAmount = data.HpFraction;

            if (DoubleBadge != null) DoubleBadge.SetActive(data.ShowOffense && data.CanDouble);
            if (AtkValue != null)  AtkValue.text  = data.ShowOffense ? data.Atk.ToString() : "—";
            if (HitValue != null)  HitValue.text  = data.ShowOffense ? data.Hit + "%" : "—";
            if (CritValue != null) CritValue.text = data.ShowOffense ? data.Crit + "%" : "—";
        }

        // Portraits face left by default; flip so this side looks toward the foe.
        private void RenderPortrait(SideModel data)
        {
            if (Portrait == null) return;
            if (data.Portrait != null) Portrait.sprite = data.Portrait;
            var prt = Portrait.rectTransform;
            float mag = Mathf.Abs(prt.localScale.x) < 0.0001f ? 1f : Mathf.Abs(prt.localScale.x);
            var ls = prt.localScale;
            ls.x = data.FlipPortrait ? -mag : mag;
            prt.localScale = ls;
        }

        private Color TintFor(Faction faction)
        {
            switch (faction)
            {
                case Faction.Enemy:  return EnemyTint;
                case Faction.Allied: return AlliedTint;
                default:             return PlayerTint;
            }
        }

        private Sprite SigilFor(WeaponType type, bool hasWeapon)
        {
            if (!hasWeapon) return null;
            switch (type)
            {
                case WeaponType.Sword:     return SigilSword;
                case WeaponType.Lance:     return SigilLance;
                case WeaponType.Axe:       return SigilAxe;
                case WeaponType.Bow:       return SigilBow;
                case WeaponType.Staff:     return SigilStaff;
                case WeaponType.AnimaTome: return SigilAnima;
                case WeaponType.LightTome: return SigilLight;
                case WeaponType.DarkTome:  return SigilDark;
                default:                   return SigilSword;
            }
        }
    }
}
