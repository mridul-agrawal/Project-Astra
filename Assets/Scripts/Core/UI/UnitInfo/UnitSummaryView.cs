using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Left-column summary — portrait, name, class, level, HP, EXP, status.
    // Shared by both tabs. Passive: one Render(model), no logic.
    public sealed class UnitSummaryView : MonoBehaviour
    {
        // One icon-only status chip. §4 gives it a glyph, a border and a wash all in the status
        // colour at different strengths, so each part is tinted separately. The border is its own
        // hollow-stroke image, which keeps the tinting to plain Image.color.
        [Serializable]
        public struct StatusChipWidgets
        {
            public GameObject Root;
            public Image Fill;
            public Image Border;
            public Image Glyph;
        }

        // Maps a status onto the glyph and colour §2 gives it.
        [Serializable]
        public struct StatusStyle
        {
            public UnitStatusKind Kind;
            public Sprite Glyph;
            public Color Tint;
        }

        [Header("Identity")]
        public Image Portrait;
        public TextMeshProUGUI UnitName;
        public TextMeshProUGUI ClassLabel;
        public Image ClassIcon;
        public TextMeshProUGUI Level;
        public TextMeshProUGUI HeaderName;      // §3 echoes the name up in the title band
        public GameObject ActedChip;

        [Header("HP")]
        public TextMeshProUGUI HpValue;
        public TextMeshProUGUI HpMax;
        public Image HpFill;
        public Image HpBlockBackground;

        [Header("EXP")]
        public GameObject ExpRow;
        public TextMeshProUGUI ExpValue;
        public Image ExpFill;

        [Header("Status")]
        public GameObject StatusRow;
        public TextMeshProUGUI StatusText;
        public TextMeshProUGUI StatusEmpty;
        public StatusChipWidgets[] StatusChips;
        public StatusStyle[] StatusStyles;

        [Header("Portrait treatment")]
        public Material ActedPortraitMaterial;
        public Sprite PlaceholderBust;

        public void Render(UnitSummaryModel model)
        {
            if (model == null) return;
            RenderIdentity(model);
            RenderHp(model);
            RenderExp(model);
            RenderStatus(model);
        }

        private void RenderIdentity(UnitSummaryModel model)
        {
            if (UnitName != null) UnitName.text = model.UnitName;
            if (HeaderName != null) HeaderName.text = model.UnitName;
            if (ClassLabel != null) ClassLabel.text = model.ClassName;
            if (ClassIcon != null && model.ClassIcon != null) ClassIcon.sprite = model.ClassIcon;
            if (Level != null) Level.text = "LV " + model.Level;
            if (ActedChip != null) ActedChip.SetActive(model.IsActed);
            RenderPortrait(model);
        }

        // An acted ally reads as spent through the greyscale material rather than a tint, so a
        // portrait with its own colours does not just get muddied.
        private void RenderPortrait(UnitSummaryModel model)
        {
            if (Portrait == null) return;

            Sprite art = model.Portrait != null ? model.Portrait : PlaceholderBust;
            if (art != null) Portrait.sprite = art;
            Portrait.material = model.IsActed ? ActedPortraitMaterial : null;
        }

        private void RenderHp(UnitSummaryModel model)
        {
            if (HpValue != null) HpValue.text = model.CurrentHP.ToString();
            if (HpMax != null) HpMax.text = "/ " + model.MaxHP;
            if (HpFill != null) HpFill.fillAmount = model.HpFraction;
        }

        private void RenderExp(UnitSummaryModel model)
        {
            if (ExpRow != null) ExpRow.SetActive(model.ShowExp);
            if (!model.ShowExp) return;

            if (ExpValue != null) ExpValue.text = model.ExpText;
            if (ExpFill != null) ExpFill.fillAmount = model.ExpFraction;
        }

        // The row itself always stays up — §4 wants an em-dash when the unit is clear, not a gap.
        private void RenderStatus(UnitSummaryModel model)
        {
            if (StatusRow != null) StatusRow.SetActive(true);
            if (StatusText != null) StatusText.text = "STATUS";

            int shown = model.Statuses != null ? model.Statuses.Count : 0;
            if (StatusEmpty != null) StatusEmpty.gameObject.SetActive(shown == 0);
            if (StatusChips == null) return;

            for (int i = 0; i < StatusChips.Length; i++)
                RenderChip(StatusChips[i], i < shown ? model.Statuses[i] : (UnitStatusKind?)null);
        }

        private void RenderChip(StatusChipWidgets chip, UnitStatusKind? kind)
        {
            if (chip.Root != null) chip.Root.SetActive(kind.HasValue);
            if (!kind.HasValue) return;

            StatusStyle style = StyleFor(kind.Value);
            if (chip.Glyph != null)
            {
                if (style.Glyph != null) chip.Glyph.sprite = style.Glyph;
                chip.Glyph.color = style.Tint;
            }
            if (chip.Border != null) chip.Border.color = style.Tint;
            if (chip.Fill != null) chip.Fill.color = Tinted(style.Tint, 0.13f);
        }

        private StatusStyle StyleFor(UnitStatusKind kind)
        {
            if (StatusStyles != null)
                foreach (var style in StatusStyles)
                    if (style.Kind == kind) return style;

            return new StatusStyle { Kind = kind, Tint = Color.white };
        }

        private static Color Tinted(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
