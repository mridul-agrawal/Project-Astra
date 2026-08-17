using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Presentation for the Objectives panel: a permanent 24px tab (bullseye + O keycap) that, while
    // the objectives key is held, slides a banner out from the same screen edge.
    //
    // The banner is content-driven. WIN and LOSE always render; the OBJECTIVES section and its rows
    // appear only when the map actually authored side objectives, so a plain map shows two lines and
    // nothing else - §B6 calls that small-is-correct rather than something to pad out.
    //
    // Which corner the panel docks to is not decided here. The composition root resolves it from the
    // cursor's quadrant and passes it down; this view only reads it.
    public sealed class ObjectiveView : MonoBehaviour
    {
        // One checklist row: box, tick, wrapping text, and an optional n/m counter.
        [Serializable]
        public struct ObjectiveRowWidgets
        {
            public GameObject Root;
            public Image Box;
            public Image BoxBorder;
            public Image Check;
            public TextMeshProUGUI Text;
            public TextMeshProUGUI Counter;
        }

        [Header("Tab (always visible)")]
        public GameObject Nub;
        public TextMeshProUGUI NubTurn;          // §A drops the turn readout; kept wired, hidden
        public GameObject PeekButtonIcon;

        [Header("Tab — spec widgets")]
        public Image BullseyeRings;
        public Image BullseyeDot;
        public GameObject KeycapChip;
        public TextMeshProUGUI KeycapLabel;

        [Header("Banner (slides in on peek)")]
        public GameObject Expanded;
        public TextMeshProUGUI WinText;
        public TextMeshProUGUI LoseText;
        public TextMeshProUGUI TurnValue;        // §B drops both; kept wired, hidden
        public TextMeshProUGUI EnemiesValue;

        [Header("Banner — spec widgets")]
        public TextMeshProUGUI WinHeader;
        public TextMeshProUGUI LoseHeader;
        public GameObject ObjectivesSection;
        public TextMeshProUGUI ObjectivesHeader;
        public ObjectiveRowWidgets[] ObjectiveRows;

        [Header("§B5 colours")]
        public Color IncompleteText = Color.white;
        public Color CompleteText = new Color32(0x8B, 0x93, 0xA0, 0xFF);
        public Color CheckboxFill = new Color32(0x4F, 0xD6, 0xF7, 0xFF);
        public Color CheckboxBorder = new Color32(0xFF, 0xFF, 0xFF, 0x80);

        [Header("Placement / Slide")]
        public float EdgePad = 56f;
        public float SlideSeconds = 0.18f;       // §B1

        // §B2 parks the banner fully past the edge: its own width plus the tab it hides behind.
        private const float TabWidth = 24f * 4f;

        // §B2's 170px ceiling, less the plate padding — and, for a row, less the box, gaps and counter.
        private const float EntryWidthCap = (170f - 16f) * 4f;
        private const float RowTextWidthCap = (170f - 16f - 8f - 5f - 20f - 5f) * 4f;

        // §A4 corner-switch dressing, in canvas px and seconds.
        private const float SwitchDistance = 230f * 4f;
        private const float SwitchExitSeconds = 0.09f;
        private const float SwitchEnterSeconds = 0.12f;

        private RectTransform panelRect;
        private RectTransform expandedRect;
        private HudCorner corner;
        private HudCorner pendingCorner;
        private bool cornerInit;
        private float bannerTop;
        private Vector2 restPosition;
        private Coroutine slide;
        private Coroutine switching;
        private bool peeking;

        private void Awake()
        {
            Bind();
            if (expandedRect == null) return;

            expandedRect.anchoredPosition = HiddenPos();
            Expanded.SetActive(false);
        }

        // Resolved on demand, not only in Awake. This panel starts inactive, so Awake does not run
        // until something switches it on - and the first Render can arrive before that, when docking
        // and the banner's resting position are both already needed.
        private void Bind()
        {
            if (panelRect == null) panelRect = GetComponent<RectTransform>();
            if (expandedRect != null || Expanded == null) return;

            expandedRect = Expanded.GetComponent<RectTransform>();
            // Only the vertical part of the authored position is kept: §B2 aligns the banner's top
            // with the tab's, but its inset is one tab-width from whichever edge it is docked to, so
            // the horizontal part has to be derived rather than remembered.
            bannerTop = expandedRect.anchoredPosition.y;
        }

        // Toggles the content rather than the GameObject: this view runs its own slide
        // coroutine, and a disabled object would kill it mid-peek. §C wants this instant.
        public void SetVisible(bool visible)
        {
            if (Nub != null) Nub.SetActive(visible);
            if (!visible && Expanded != null) Expanded.SetActive(false);
        }

        public void Render(ObjectiveModel model)
        {
            if (model == null) return;
            Bind();

            RenderConditions(model);
            RenderObjectives(model);
            ApplyCorner(model.Corner);
        }

        // ---- content -------------------------------------------------------------------------

        private void RenderConditions(ObjectiveModel model)
        {
            SetWrapped(WinText, model.WinText, EntryWidthCap);
            SetWrapped(LoseText, model.LoseText, EntryWidthCap);

            // §B3 fixes the header words; only the entry lines come from the map.
            if (WinHeader != null) WinHeader.text = "WIN";
            if (LoseHeader != null) LoseHeader.text = "LOSE";
        }

        // §B2 makes the plate exactly as wide as its widest line, with 170px only as a ceiling. A
        // ContentSizeFitter has no maximum, so each wrapping line is clamped to the cap here and the
        // plate hugs whatever the widest one turns out to be.
        private static void SetWrapped(TextMeshProUGUI label, string text, float cap)
        {
            if (label == null) return;

            label.text = text;
            var sizer = label.GetComponent<LayoutElement>();
            if (sizer == null) return;

            sizer.preferredWidth = Mathf.Min(label.GetPreferredValues(text).x, cap);
        }

        // The whole section stands down on a map with no side objectives, header included.
        private void RenderObjectives(ObjectiveModel model)
        {
            bool any = model.HasObjectives;
            if (ObjectivesSection != null) ObjectivesSection.SetActive(any);
            if (ObjectivesHeader != null) ObjectivesHeader.text = "OBJECTIVES";
            if (ObjectiveRows == null) return;

            List<ObjectiveRowVM> rows = model.Objectives;
            int shown = any ? Mathf.Min(rows.Count, ObjectiveRows.Length) : 0;

            if (any && rows.Count > ObjectiveRows.Length)
                Debug.LogWarning($"[ObjectiveView] Map authored {rows.Count} objectives; " +
                                 $"only {ObjectiveRows.Length} rows exist, so the rest are not shown.");

            for (int i = 0; i < ObjectiveRows.Length; i++)
            {
                bool used = i < shown;
                if (ObjectiveRows[i].Root != null) ObjectiveRows[i].Root.SetActive(used);
                if (used) RenderRow(ObjectiveRows[i], rows[i]);
            }
        }

        private void RenderRow(ObjectiveRowWidgets ui, ObjectiveRowVM row)
        {
            if (ui.Text != null)
            {
                SetWrapped(ui.Text, row.Text, RowTextWidthCap);
                ui.Text.color = row.Complete ? CompleteText : IncompleteText;
            }

            RenderCheckbox(ui, row.Complete);

            if (ui.Counter == null) return;
            ui.Counter.gameObject.SetActive(row.HasCounter);
            if (row.HasCounter) ui.Counter.text = row.CounterText;
        }

        // §B5: complete is a solid cyan box with a dark tick and no border; incomplete is an empty
        // box with a hairline. The border is its own image so neither state needs a colour with alpha
        // doing double duty.
        private void RenderCheckbox(ObjectiveRowWidgets ui, bool complete)
        {
            if (ui.Box != null)
            {
                ui.Box.enabled = complete;
                ui.Box.color = CheckboxFill;
            }
            if (ui.BoxBorder != null)
            {
                ui.BoxBorder.enabled = !complete;
                ui.BoxBorder.color = CheckboxBorder;
            }
            if (ui.Check != null) ui.Check.enabled = complete;
        }

        // ---- placement -----------------------------------------------------------------------

        // §A4's motion rule: an edge element may only move perpendicular to its own edge, never
        // laterally across the screen. Corner docking is already an instant teleport, so what this
        // adds is the dressing around it - slide into the current edge, teleport, slide in from the
        // new one. The teleport itself is still HudCornerLayout's, untouched.
        private void ApplyCorner(HudCorner target)
        {
            // First placement of the run: dock with no theatre.
            if (!cornerInit)
            {
                cornerInit = true;
                pendingCorner = target;
                Dock(target);
                return;
            }

            // Compared against the pending target, not the current corner: while a switch is in
            // flight, a cursor that crosses back has to register as a revert rather than being
            // swallowed as "no change".
            if (target == pendingCorner) return;
            pendingCorner = target;

            if (!Application.isPlaying)
            {
                Dock(target);
                return;
            }

            // A switch already running picks the new target up on its next lap; no queuing.
            if (switching == null) switching = StartCoroutine(SwitchCorner());
        }

        private void Dock(HudCorner target)
        {
            Bind();
            corner = target;
            if (panelRect == null) return;

            HudCornerLayout.Apply(panelRect, target, EdgePad);
            restPosition = panelRect.anchoredPosition;
            HugSide(target);
        }

        // Loops rather than recursing, so a target that keeps changing mid-flight just earns another
        // lap and the panel is never rendered between two anchors.
        private IEnumerator SwitchCorner()
        {
            while (true)
            {
                yield return SlidePanel(restPosition, EdgeOffset(corner), SwitchExitSeconds, EaseIn);

                // Reverted mid-exit: there is nowhere new to go, so re-enter the corner we left.
                if (pendingCorner != corner)
                {
                    Dock(pendingCorner);
                    panelRect.anchoredPosition = EdgeOffset(corner);
                }

                yield return SlidePanel(panelRect.anchoredPosition, restPosition,
                                        SwitchEnterSeconds, EaseOut);
                panelRect.anchoredPosition = restPosition;

                if (pendingCorner == corner) break;
            }
            switching = null;
        }

        private IEnumerator SlidePanel(Vector2 from, Vector2 to, float seconds, Func<float, float> ease)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                panelRect.anchoredPosition = Vector2.Lerp(from, to, ease(t / seconds));
                yield return null;
            }
            panelRect.anchoredPosition = to;
        }

        // Off the panel's own screen edge — right corner exits right, left corner exits left.
        private Vector2 EdgeOffset(HudCorner target) =>
            restPosition + new Vector2(OnLeft(target) ? -SwitchDistance : SwitchDistance, 0f);

        // cubic-bezier(0.4, 0, 1, 1) — §A4's ease-in.
        private static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        // Tab and banner hang from the docked corner and open inward, so their pivot follows
        // whichever top corner the panel sits in.
        private void HugSide(HudCorner target)
        {
            Vector2 pivot = OnLeft(target) ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            if (Nub != null) ((RectTransform)Nub.transform).pivot = pivot;
            if (expandedRect == null) return;

            expandedRect.pivot = pivot;
            MirrorKeycap(target);

            // Re-staged whether or not the banner is open: a corner switch mid-peek flips the pivot,
            // and leaving the old offset behind would drop the banner in the wrong place.
            expandedRect.anchoredPosition = peeking ? ShownPos() : HiddenPos();
        }

        // §A2 pins the keycap to the tab's outer, screen-edge side, so it swaps with the corner.
        private void MirrorKeycap(HudCorner target)
        {
            if (KeycapChip == null) return;

            var rect = (RectTransform)KeycapChip.transform;
            Vector2 anchor = OnLeft(target) ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            float inset = 1f * 4f;

            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(OnLeft(target) ? inset : -inset, inset);
        }

        private static bool OnLeft(HudCorner target) =>
            target == HudCorner.TopLeft || target == HudCorner.BottomLeft;

        // ---- §B1 hold-to-peek -----------------------------------------------------------------

        public void SetPeek(bool peek)
        {
            Bind();
            if (expandedRect == null) return;

            peeking = peek;
            if (slide != null) StopCoroutine(slide);
            slide = StartCoroutine(Slide(peek));
        }

        // Retargets from wherever the banner currently sits rather than restarting, so tapping the
        // key on and off does not jump.
        private IEnumerator Slide(bool peek)
        {
            if (peek) Expanded.SetActive(true);

            Vector2 from = expandedRect.anchoredPosition;
            Vector2 to = peek ? ShownPos() : HiddenPos();

            for (float t = 0f; t < SlideSeconds; t += Time.unscaledDeltaTime)
            {
                expandedRect.anchoredPosition = Vector2.Lerp(from, to, EaseOut(t / SlideSeconds));
                yield return null;
            }

            expandedRect.anchoredPosition = to;
            if (!peek) Expanded.SetActive(false);
            slide = null;
        }

        // §B2: the banner's inner edge sits one tab-width in from the screen edge it shares with the
        // tab, so the offset flips sign with the corner.
        private Vector2 ShownPos() =>
            new Vector2(OnLeft(corner) ? TabWidth : -TabWidth, bannerTop);

        // Parked fully past the docked edge - its own width plus the tab - so it comes in sideways.
        private Vector2 HiddenPos()
        {
            float dx = expandedRect.rect.width + TabWidth;
            Vector2 shown = ShownPos();
            return new Vector2(shown.x + (OnLeft(corner) ? -dx : dx), shown.y);
        }

        // cubic-bezier(0, 0, 0.58, 1) — §B1's ease-out, close enough at this duration.
        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }
    }
}
