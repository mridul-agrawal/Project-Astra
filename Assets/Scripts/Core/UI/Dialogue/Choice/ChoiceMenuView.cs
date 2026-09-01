using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.UI.Dialogue.Choice
{
    // Draws the choice list under the dialogue box. Render-only: it owns no input and no selection
    // state, which is the point.
    //
    // Not SelectionMenuView, deliberately. That one subscribes to InputManager itself — the second
    // subscriber a confirm press would reach, alongside the dialogue service — hides itself on every
    // pick, and flips a static HasInputFocus flag the battle-map cursor reads. All three are wrong
    // here.
    public sealed class ChoiceMenuView : MonoBehaviour
    {
        public GameObject content;
        public RectTransform rowContainer;
        public GameObject rowTemplate;

        public Color enabledColor = Color.white;
        public Color disabledColor = new(0.45f, 0.45f, 0.45f, 1f);
        public Color highlightColor = new(1f, 0.87f, 0.45f, 1f);

        private readonly List<TextMeshProUGUI> rows = new();

        // What DialogueRunner hands down: the options as they should read, and which one the
        // cursor is on. No selection state of its own — that is the point.
        public void Render(IReadOnlyList<DialogueChoiceView> options, int highlighted)
        {
            SetVisible(options != null && options.Count > 0);
            if (options == null || options.Count == 0) return;

            EnsureRowCount(options.Count);
            for (int i = 0; i < rows.Count; i++)
                RenderRow(rows[i], i < options.Count, i < options.Count && options[i].Enabled,
                    i == highlighted, i < options.Count ? options[i].Label : null);
        }

        public void SetVisible(bool visible)
        {
            if (content != null) content.SetActive(visible);
        }

        private void RenderRow(TextMeshProUGUI row, bool used, bool enabled, bool isHighlighted, string label)
        {
            row.gameObject.SetActive(used);
            if (!used) return;

            row.text = isHighlighted ? "› " + label : "   " + label;
            row.color = !enabled ? disabledColor : isHighlighted ? highlightColor : enabledColor;
        }

        // Rows are cloned once and then reused, so reopening a menu doesn't churn the hierarchy.
        // A view missing its template draws nothing rather than throwing once per frame.
        private void EnsureRowCount(int needed)
        {
            if (rowTemplate == null || rowContainer == null) return;

            while (rows.Count < needed)
            {
                GameObject clone = Instantiate(rowTemplate, rowContainer);
                clone.name = $"Row_{rows.Count}";
                clone.SetActive(true);
                rows.Add(clone.GetComponent<TextMeshProUGUI>());
            }
        }
    }
}
