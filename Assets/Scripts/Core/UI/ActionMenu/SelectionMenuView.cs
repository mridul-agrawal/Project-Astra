using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.UI.ActionMenu
{
    // Generic vertical list-menu popup driven by a list of string options.
    // Reused for the unit action menu (Attack/Item/Trade/Wait…), the trade-
    // target picker, and the inventory slot sub-menu.
    //
    // The look lives in the authored SelectionMenu prefab — background, the
    // disabled Row_Template child, colours and spacing. This component only
    // clones that template once per option into the layout-group container and
    // drives selection + input; it builds no hierarchy of its own.
    //
    // Lifecycle: Show(options, onSelect, onCancel) → input → Hide().
    // HasInputFocus gates the grid cursor while the menu is up.
    public class SelectionMenuView : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [Header("Widgets")]
        [SerializeField] private RectTransform optionsContainer;   // layout-group parent for cloned rows
        [SerializeField] private GameObject rowTemplate;           // disabled Row_Template child
        [SerializeField] private Material selectedGlowMat;
        [SerializeField] private TMP_FontAsset optionFont;         // base material for unselected rows

        [Header("Text palette")]
        [SerializeField] private Color textDefault  = new Color32(0xf5, 0xd8, 0xb8, 0xff);
        [SerializeField] private Color textSelected = new Color32(0xf5, 0xd8, 0xb8, 0xff);
        [SerializeField] private Color textDisabled = new Color32(0x4a, 0x2a, 0x1a, 0xff);

        private readonly List<TextMeshProUGUI> rowTexts = new();
        private readonly List<GameObject> rowAccents = new();
        private readonly List<GameObject> rowTrishuls = new();
        private readonly List<GameObject> spawnedRows = new();

        private int selectedIndex;
        private List<string> options;
        private List<bool> enabledFlags;
        private Action<int> onSelect;
        private Action onCancel;

        public void Show(List<string> options, Action<int> onSelect, Action onCancel, List<bool> enabledFlags = null)
        {
            this.options = options;
            this.onSelect = onSelect;
            this.onCancel = onCancel;
            this.enabledFlags = enabledFlags;
            selectedIndex = FindFirstEnabled(0, 1);

            gameObject.SetActive(true);
            BuildRows();
            UpdateSelection();

            HasInputFocus = true;
            Subscribe();

            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        public void Hide()
        {
            HasInputFocus = false;
            Unsubscribe();
            ClearRows();
            onSelect = null;
            onCancel = null;
            gameObject.SetActive(false);
        }

        // Safety net if the panel is deactivated without going through Hide.
        private void OnDisable()
        {
            if (!HasInputFocus) return;
            HasInputFocus = false;
            Unsubscribe();
        }

        // --- Input handling ---

        private void Subscribe()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += Confirm;
            InputManager.Instance.OnCancel += Cancel;
        }

        private void Unsubscribe()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= Confirm;
            InputManager.Instance.OnCancel -= Cancel;
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y == 0) return;
            int step = dir.y > 0 ? -1 : 1;
            int next = FindFirstEnabled(selectedIndex + step, step);
            if (next >= 0)
            {
                selectedIndex = next;
                UpdateSelection();
                AudioManager.Instance?.Play(SoundId.UiMove);
            }
        }

        private void Confirm()
        {
            if (IsDisabled(selectedIndex)) { AudioManager.Instance?.Play(SoundId.UiInvalid); return; }
            AudioManager.Instance?.Play(SoundId.ConfirmAction);
            int index = selectedIndex;
            var callback = onSelect;
            Hide();
            callback?.Invoke(index);
        }

        private void Cancel()
        {
            AudioManager.Instance?.Play(SoundId.CancelGrid);
            var callback = onCancel;
            Hide();
            callback?.Invoke();
        }

        private bool IsDisabled(int index) =>
            enabledFlags != null && index < enabledFlags.Count && !enabledFlags[index];

        private int FindFirstEnabled(int start, int step)
        {
            if (options == null || options.Count == 0) return 0;
            int count = options.Count;
            int idx = ((start % count) + count) % count;
            for (int i = 0; i < count; i++)
            {
                if (!IsDisabled(idx)) return idx;
                idx = ((idx + step) % count + count) % count;
            }
            return 0;
        }

        // --- Rows (clone the authored template) ---

        private void BuildRows()
        {
            ClearRows();
            if (rowTemplate == null || optionsContainer == null || options == null) return;

            int count = options.Count;
            for (int i = 0; i < count; i++)
            {
                var row = Instantiate(rowTemplate, optionsContainer);
                row.name = options[i];
                row.SetActive(true);
                BindRow(row.transform, options[i], IsDisabled(i), isLast: i == count - 1);
                spawnedRows.Add(row);
            }
        }

        private void BindRow(Transform row, string label, bool disabled, bool isLast)
        {
            var text = row.Find("Text").GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.color = disabled ? textDisabled : textDefault;
            rowTexts.Add(text);

            var accent = row.Find("AccentBar").gameObject;
            accent.SetActive(false);
            rowAccents.Add(accent);

            var trishul = row.Find("Trishul").gameObject;
            trishul.SetActive(false);
            rowTrishuls.Add(trishul);

            var divider = row.Find("Divider");
            if (divider != null) divider.gameObject.SetActive(!isLast);
        }

        private void ClearRows()
        {
            foreach (var row in spawnedRows)
                if (row != null) Destroy(row);
            spawnedRows.Clear();
            rowTexts.Clear();
            rowAccents.Clear();
            rowTrishuls.Clear();
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < rowTexts.Count; i++)
            {
                bool selected = i == selectedIndex;
                bool disabled = IsDisabled(i);

                rowTexts[i].color = disabled ? textDisabled
                    : selected ? textSelected : textDefault;

                if (selected && !disabled && selectedGlowMat != null)
                    rowTexts[i].fontMaterial = selectedGlowMat;
                else if (optionFont != null)
                    rowTexts[i].fontMaterial = optionFont.material;
                // else: leave material as-is; assigning null to TMP.fontMaterial throws NRE.

                rowAccents[i].SetActive(selected && !disabled);
                rowTrishuls[i].SetActive(selected && !disabled);
            }
        }
    }
}
