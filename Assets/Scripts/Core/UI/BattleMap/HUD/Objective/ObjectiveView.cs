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

        private RectTransform panelRect;
        private RectTransform expandedRect;
        private HudCorner corner;
        private bool cornerInit;
        private Vector2 shownPos;
        private Coroutine slide;
        private bool peeking;

        private void Awake()
        {
            panelRect = GetComponent<RectTransform>();
            if (Expanded == null) return;

            expandedRect = Expanded.GetComponent<RectTransform>();
            shownPos = expandedRect.anchoredPosition;
            expandedRect.anchoredPosition = HiddenPos();
            Expanded.SetActive(false);
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

            RenderConditions(model);
            RenderObjectives(model);
            ApplyCorner(model.Corner);
        }

        // ---- content -------------------------------------------------------------------------

        private void RenderConditions(ObjectiveModel model)
        {
            if (WinText != null) WinText.text = model.WinText;
            if (LoseText != null) LoseText.text = model.LoseText;

            // §B3 fixes the header words; only the entry lines come from the map.
            if (WinHeader != null) WinHeader.text = "WIN";
            if (LoseHeader != null) LoseHeader.text = "LOSE";
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
                ui.Text.text = row.Text;
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

        private void ApplyCorner(HudCorner target)
        {
            if (cornerInit && target == corner) return;
            corner = target;
            cornerInit = true;
            HudCornerLayout.Apply(panelRect, target, EdgePad);
            HugSide(target);
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
            expandedRect.anchoredPosition = peeking ? shownPos : HiddenPos();
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
            Vector2 to = peek ? shownPos : HiddenPos();

            for (float t = 0f; t < SlideSeconds; t += Time.unscaledDeltaTime)
            {
                expandedRect.anchoredPosition = Vector2.Lerp(from, to, EaseOut(t / SlideSeconds));
                yield return null;
            }

            expandedRect.anchoredPosition = to;
            if (!peek) Expanded.SetActive(false);
            slide = null;
        }

        // Parked past the docked side edge, at the shown height, so it comes in sideways.
        private Vector2 HiddenPos()
        {
            float dx = expandedRect.rect.width + TabWidth;
            return new Vector2(shownPos.x + (OnLeft(corner) ? -dx : dx), shownPos.y);
        }

        // cubic-bezier(0, 0, 0.58, 1) — §B1's ease-out, close enough at this duration.
        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }
    }
}
