using System.Collections;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Presentation for the Objective panel. A small always-visible nub (turn number +
    // peek-button glyph) that, on hold-to-peek, slides an expanded panel down with the
    // full mission detail. The whole panel docks to a top corner chosen by the controller.
    public sealed class ObjectiveView : MonoBehaviour
    {
        [Header("Nub (always visible)")]
        public GameObject Nub;
        public TextMeshProUGUI NubTurn;
        public GameObject PeekButtonIcon;

        [Header("Expanded (slides in on peek)")]
        public GameObject Expanded;
        public TextMeshProUGUI WinText;
        public TextMeshProUGUI LoseText;
        public TextMeshProUGUI TurnValue;
        public TextMeshProUGUI EnemiesValue;

        [Header("Placement / Slide")]
        public float EdgePad = 56f;
        public float SlideSeconds = 0.18f;

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

        public void Render(ObjectiveModel model)
        {
            if (model == null) return;
            if (NubTurn != null)      NubTurn.text      = model.Turn.ToString("00");
            if (TurnValue != null)    TurnValue.text    = model.Turn.ToString("00");
            if (EnemiesValue != null) EnemiesValue.text = model.EnemiesRemaining.ToString();
            if (WinText != null)      WinText.text      = model.WinText;
            if (LoseText != null)     LoseText.text     = model.LoseText;
            ApplyCorner(model.Corner);
        }

        private void ApplyCorner(HudCorner target)
        {
            if (cornerInit && target == corner) return;
            corner = target;
            cornerInit = true;
            HudCornerLayout.Apply(panelRect, target, EdgePad);
            HugSide(target);
        }

        // Nub and expanded hang from the docked corner and open inward, so their pivot
        // follows whichever top corner the panel sits in.
        private void HugSide(HudCorner target)
        {
            Vector2 pivot = target == HudCorner.TopLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            if (Nub != null) ((RectTransform)Nub.transform).pivot = pivot;
            if (expandedRect == null) return;
            expandedRect.pivot = pivot;
            if (!peeking) expandedRect.anchoredPosition = HiddenPos();  // re-stage off the new edge
        }

        // Hold-to-peek: slide the expanded panel down into view, or back up out of it.
        public void SetPeek(bool peek)
        {
            if (expandedRect == null) return;
            peeking = peek;
            if (slide != null) StopCoroutine(slide);
            slide = StartCoroutine(Slide(peek));
        }

        private IEnumerator Slide(bool peek)
        {
            if (peek) Expanded.SetActive(true);
            Vector2 from = expandedRect.anchoredPosition;
            Vector2 to = peek ? shownPos : HiddenPos();
            for (float t = 0f; t < SlideSeconds; t += Time.unscaledDeltaTime)
            {
                expandedRect.anchoredPosition = Vector2.Lerp(from, to, t / SlideSeconds);
                yield return null;
            }
            expandedRect.anchoredPosition = to;
            if (!peek) Expanded.SetActive(false);
            slide = null;
        }

        // Hidden = slid off the docked side edge (a left corner hides to the left, a
        // right corner to the right), staying at the shown height so it comes in sideways.
        private Vector2 HiddenPos()
        {
            bool onLeft = corner == HudCorner.TopLeft || corner == HudCorner.BottomLeft;
            float dx = expandedRect.rect.width + EdgePad + 40f;
            return new Vector2(shownPos.x + (onLeft ? -dx : dx), shownPos.y);
        }
    }
}
