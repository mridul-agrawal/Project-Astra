using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // The tile info panel: a name plate that is always up, plus up to three effect strips.
    //
    // Widgets are pooled and pre-placed by the restyle pass; this view only switches them on and
    // fills them in. Content changes are a cut on the same frame per §7 - the only animation is
    // the 120 ms fade when the panel goes from hidden to visible.
    //
    // Which corner the panel docks to is not decided here. The composition root resolves it from
    // the cursor's quadrant and passes it down; this view only reads it, to dock and to mirror.
    public sealed class TileInfoView : MonoBehaviour
    {
        [Serializable]
        public struct ChipWidgets
        {
            public GameObject Root;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Value;
        }

        [Serializable]
        public struct StripWidgets
        {
            public GameObject Root;
            public Image Background;
            public Image Chevron;
            public HorizontalLayoutGroup Row;
            public ChipWidgets[] Chips;
        }

        [Header("Widgets")]
        public GameObject Root;
        public TextMeshProUGUI TileName;

        [Header("Legacy widgets (kept wired, hidden by the spec pass)")]
        public GameObject StatDef;
        public TextMeshProUGUI StatValueDef;
        public GameObject StatAvo;
        public TextMeshProUGUI StatValueAvo;
        public GameObject StatDivider;
        public TextMeshProUGUI HealValue;

        [Header("Spec widgets")]
        public GameObject NamePlate;
        public LayoutElement NameSizer;
        public GameObject StripsContainer;
        public VerticalLayoutGroup Column;
        public StripWidgets[] Strips;
        public CanvasGroup Fader;

        [Header("§4 chip weights")]
        public TMP_FontAsset LabelFont;      // stat labels, weight 500
        public TMP_FontAsset FlagFont;       // flag words, weight 600

        [Header("§5 colours")]
        public Color PositiveValue = new Color32(0x4F, 0xD6, 0xF7, 0xFF);
        public Color NegativeValue = new Color32(0xFF, 0x5A, 0x56, 0xFF);
        public Color FlagText = Color.white;
        public Color ChevronNormal = new Color32(0xFF, 0xFF, 0xFF, 0xB3);
        public Color ChevronNegative = new Color32(0xFF, 0x5A, 0x56, 0xFF);

        // §3 panel maximum 190px, less the name plate's 10px padding either side, at the 4x scale.
        private const float NameMaxWidth = (190f - 20f) * 4f;

        // §7 entrance: fade in over 120 ms while rising 4 spec px.
        private const float EntranceTime = 0.12f;
        private const float RiseDistance = 4f * 4f;

        private const float EdgePad = 56f;

        private RectTransform cachedRect;
        private HudCorner corner;
        private bool cornerInit;
        private bool shown;
        private Coroutine motion;
        private Vector2 dockedPosition;
        private bool dockedCaptured;

        // Resolved on demand rather than in Awake. The panel starts inactive, so Awake does not run
        // until something switches it on - and docking has to work on the very first render, before
        // that happens.
        private RectTransform Rect
        {
            get
            {
                if (cachedRect == null)
                    cachedRect = Root != null ? Root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
                return cachedRect;
            }
        }

        // Driven by the composition root when the whole HUD steps aside for a phase banner.
        public void SetVisible(bool visible)
        {
            if (visible) BeginEntrance();
            else BeginExit();
        }

        public void Render(TileInfoModel model)
        {
            if (model == null)
            {
                BeginExit();
                return;
            }

            ApplyCorner(model.Corner);
            RenderName(model.TerrainName);
            RenderStrips(model);
            Mirror(model.Corner);
            BeginEntrance();
        }

        // ---- content -------------------------------------------------------------------------

        // §6 keeps long names on one line and lets them ellipsize at the panel maximum. The clamp
        // is applied to the label's preferred width, because a ContentSizeFitter has no maximum.
        private void RenderName(string terrainName)
        {
            if (TileName == null) return;

            TileName.text = terrainName;
            if (NameSizer == null) return;

            float wanted = TileName.GetPreferredValues(terrainName).x;
            NameSizer.preferredWidth = Mathf.Min(wanted, NameMaxWidth);
        }

        private void RenderStrips(TileInfoModel model)
        {
            if (Strips == null) return;

            int count = model.Strips != null ? model.Strips.Count : 0;
            if (StripsContainer != null) StripsContainer.SetActive(count > 0);

            for (int i = 0; i < Strips.Length; i++)
            {
                bool used = i < count;
                if (Strips[i].Root != null) Strips[i].Root.SetActive(used);
                if (used) RenderStrip(Strips[i], model.Strips[i]);
            }
        }

        private void RenderStrip(StripWidgets ui, TileEffectStrip strip)
        {
            if (ui.Chevron != null)
                ui.Chevron.color = strip.HasNegative ? ChevronNegative : ChevronNormal;

            if (ui.Chips == null) return;
            List<TileEffectChip> chips = strip.Chips;

            for (int i = 0; i < ui.Chips.Length; i++)
            {
                bool used = i < chips.Count;
                if (ui.Chips[i].Root != null) ui.Chips[i].Root.SetActive(used);
                if (used) RenderChip(ui.Chips[i], chips[i]);
            }
        }

        private void RenderChip(ChipWidgets ui, TileEffectChip chip)
        {
            if (ui.Label != null)
            {
                ui.Label.text = chip.Label;
                ui.Label.color = FlagText;

                // §4 sets flag words a weight heavier than stat labels, 600 against 500.
                TMP_FontAsset weight = chip.IsFlag ? FlagFont : LabelFont;
                if (weight != null) ui.Label.font = weight;
            }
            if (ui.Value == null) return;

            // A flag is a bare word, so its value half stands down entirely.
            ui.Value.gameObject.SetActive(!chip.IsFlag);
            if (chip.IsFlag) return;

            ui.Value.text = FormatValue(chip.Value);
            ui.Value.color = chip.Value < 0 ? NegativeValue : PositiveValue;
        }

        // §6 wants a real minus sign, U+2212, never a hyphen.
        private static string FormatValue(int value) =>
            value < 0 ? "−" + Mathf.Abs(value) : "+" + value;

        // ---- side ----------------------------------------------------------------------------

        // §3's ragged edges mirror when the panel sits on the right, so the rectangles hug the
        // corner they are docked to. The side itself comes from the placement system.
        private void Mirror(HudCorner target)
        {
            bool onLeft = target == HudCorner.TopLeft || target == HudCorner.BottomLeft;
            TextAnchor column = onLeft ? TextAnchor.LowerLeft : TextAnchor.LowerRight;

            if (Column != null) Column.childAlignment = column;
            if (TileName != null)
                TileName.alignment = onLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;

            // Only the rectangles change side. §6 keeps the chevron at the far left either way -
            // the arrow points into its own chips, which it would not do if it swapped ends.
            if (Strips == null) return;
            foreach (StripWidgets strip in Strips)
                if (strip.Row != null)
                    strip.Row.childAlignment = onLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        }

        // Tile info always sits on the bottom row; the composition root picks which
        // bottom corner. Docking is the shared corner-layout used by every HUD panel.
        private void ApplyCorner(HudCorner target)
        {
            if (Rect == null || (cornerInit && target == corner)) return;
            corner = target;
            cornerInit = true;
            HudCornerLayout.Apply(Rect, target, EdgePad);

            // Remembered so the entrance rise can offset from wherever docking left us, without
            // this view needing to know how the corner layout computes its inset.
            dockedPosition = Rect.anchoredPosition;
            dockedCaptured = true;
        }

        // ---- §7 entrance and exit -------------------------------------------------------------

        private void BeginEntrance()
        {
            StopMotion();
            if (Root != null) Root.SetActive(true);

            // Already up, or edit-time: land on the resting state without animating.
            if (shown || !Application.isPlaying)
            {
                shown = true;
                SetFade(1f, 0f);
                return;
            }

            shown = true;
            motion = StartCoroutine(PlayEntrance());
        }

        private void BeginExit()
        {
            if (!shown)
            {
                if (Root != null && !Application.isPlaying) Root.SetActive(false);
                return;
            }
            StopMotion();

            if (!Application.isPlaying)
            {
                shown = false;
                if (Root != null) Root.SetActive(false);
                return;
            }

            motion = StartCoroutine(PlayExit());
        }

        private IEnumerator PlayEntrance()
        {
            float time = 0f;
            while (time < EntranceTime)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(time / EntranceTime));
                SetFade(t, RiseDistance * (1f - t));
                yield return null;
            }
            SetFade(1f, 0f);
            motion = null;
        }

        private IEnumerator PlayExit()
        {
            float time = 0f;
            while (time < EntranceTime)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(time / EntranceTime));
                SetFade(1f - t, RiseDistance * t);
                yield return null;
            }

            shown = false;
            SetFade(0f, 0f);
            if (Root != null) Root.SetActive(false);
            motion = null;
        }

        private void StopMotion()
        {
            if (motion == null) return;
            StopCoroutine(motion);
            motion = null;
        }

        private void SetFade(float alpha, float rise)
        {
            if (Fader != null) Fader.alpha = alpha;
            if (Rect == null) return;

            // Before the first dock the panel is wherever the scene left it, so that stands in as
            // the resting position instead of snapping to the origin.
            if (!dockedCaptured)
            {
                dockedPosition = Rect.anchoredPosition;
                dockedCaptured = true;
            }
            Rect.anchoredPosition = dockedPosition + new Vector2(0f, rise);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
