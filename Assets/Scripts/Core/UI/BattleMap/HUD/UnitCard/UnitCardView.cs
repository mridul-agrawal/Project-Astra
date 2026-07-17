using System.Collections.Generic;
using ProjectAstra.Core.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class UnitCardView : MonoBehaviour
    {
        [Header("Widgets")]
        public GameObject Root;
        public TextMeshProUGUI UnitName;
        public TextMeshProUGUI HpValue;
        public TextMeshProUGUI LvlValue;
        public Image HpFill;
        public Image PortraitImage;

        // HP threshold colours (mockup gradient #2e7a3a -> #8de078).
        private static readonly Color HpGreen  = new Color32(0x8d, 0xe0, 0x78, 0xff);
        private static readonly Color HpYellow = new Color32(0xf0, 0xd0, 0x60, 0xff);
        private static readonly Color HpRed    = new Color32(0xe8, 0x40, 0x2a, 0xff);

        private Image[] cornerBosses;

        private void Awake() => CacheCornerBosses();

        public void Render(UnitCardModel model)
        {
            if (model == null)
            {
                if (Root != null) Root.SetActive(false);
                return;
            }
            if (Root != null) Root.SetActive(true);

            if (UnitName != null)   UnitName.text   = model.UnitName;
            if (HpValue != null)    HpValue.text    = model.CurrentHP + " / " + model.MaxHP;
            if (LvlValue != null)   LvlValue.text   = model.UnitLevel.ToString();

            if (HpFill != null)
            {
                HpFill.fillAmount = model.HpFraction;
                HpFill.color      = HpColorForFraction(model.HpFraction);
            }

            TintCornerBosses(FactionTint(model.UnitFaction));

            if (PortraitImage != null && model.unitCardPortriat != null)
                PortraitImage.sprite = model.unitCardPortriat;
        }

        private static Color HpColorForFraction(float frac)
        {
            if (frac > 0.50f) return HpGreen;
            if (frac > 0.25f) return HpYellow;
            return HpRed;
        }

        private static Color FactionTint(Faction f)
        {
            switch (f)
            {
                case Faction.Enemy:  return new Color(1.00f, 0.45f, 0.45f); // reddened gold
                case Faction.Allied: return new Color(0.55f, 0.80f, 1.00f); // bluish gold
                default:             return Color.white;                    // Player: unchanged gold
            }
        }

        private void TintCornerBosses(Color tint)
        {
            if (cornerBosses == null) return;
            for (int i = 0; i < cornerBosses.Length; i++)
                if (cornerBosses[i] != null) cornerBosses[i].color = tint;
        }

        private void CacheCornerBosses()
        {
            var host = Root != null ? Root : gameObject;
            var all = host.GetComponentsInChildren<Image>(true);
            var result = new List<Image>(4);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gameObject.name == "CornerBoss") result.Add(all[i]);
            cornerBosses = result.ToArray();
        }
    }
}
