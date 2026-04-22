using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Unit Action Menu popup — appears after a unit moves, showing context-dependent
    /// options (Attack, Item, Trade, Wait, etc.). Visual style: Warrior's Command
    /// (octagonal frame, ember gradients, trishul cursor).
    ///
    /// Lifecycle: Show(options, onSelect, onCancel) → input handling → Hide().
    /// Integration: GridCursor calls Show after movement; HasInputFocus gates cursor input.
    /// </summary>
    public class UnitActionMenuUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [Header("Visual Assets")]
        [SerializeField] private Sprite _bgSprite;
        [SerializeField] private Sprite _cursorSprite;
        [SerializeField] private Sprite _dividerSprite;
        [SerializeField] private TMP_FontAsset _optionFont;
        [SerializeField] private Material _selectedGlowMat;

        // Runtime asset transfer — used when a consumer (e.g. InventoryMenuUI) spawns a
        // sub-menu at runtime and needs it to look identical to the scene-wired template.
        public void CopyAssetsFrom(UnitActionMenuUI template)
        {
            if (template == null) return;
            _bgSprite        = template._bgSprite;
            _cursorSprite    = template._cursorSprite;
            _dividerSprite   = template._dividerSprite;
            _optionFont      = template._optionFont;
            _selectedGlowMat = template._selectedGlowMat;
        }

        // Warrior's Command palette (default). ApplyPalette overrides these per-instance
        // so spawners can reskin the menu (e.g. InventoryMenuUI → Indigo Codex parchment).
        static readonly Color DefaultTextDefault  = new Color32(0xf5, 0xd8, 0xb8, 0xff);
        static readonly Color DefaultTextSelected = new Color32(0xf5, 0xd8, 0xb8, 0xff);
        static readonly Color ColTextDisabled     = new Color32(0x4a, 0x2a, 0x1a, 0xff);
        static readonly Color DefaultEmberBar     = new Color32(0xd4, 0x6a, 0x2c, 0xff);

        Color _colTextDefault  = DefaultTextDefault;
        Color _colTextSelected = DefaultTextSelected;
        Color _colAccentBar    = DefaultEmberBar;
        Color _colBgTint       = Color.white;

        public void ApplyPalette(Color bgTint, Color textDefault, Color textSelected, Color accentBar)
        {
            _colBgTint       = bgTint;
            _colTextDefault  = textDefault;
            _colTextSelected = textSelected;
            _colAccentBar    = accentBar;
        }

        const float OptionHeight = 52f;
        const float DividerHeight = 8f;
        const float PanelPaddingY = 20f;
        const float PanelWidth = 340f;
        const float TrishulSize = 20f;
        const float TextLeftOffset = 48f;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _optionTexts = new();
        private readonly List<GameObject> _accentBars = new();
        private readonly List<GameObject> _trishulIcons = new();
        private int _selectedIndex;
        private List<string> _options;
        private List<bool> _enabledFlags;
        private Action<int> _onSelect;
        private Action _onCancel;

        public void Show(List<string> options, Action<int> onSelect, Action onCancel,
            List<bool> enabledFlags = null)
        {
            _options = options;
            _onSelect = onSelect;
            _onCancel = onCancel;
            _enabledFlags = enabledFlags;
            _selectedIndex = FindFirstEnabled(0, 1);

            BuildUI();
            UpdateSelection();

            HasInputFocus = true;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += Navigate;
                InputManager.Instance.OnConfirm += Confirm;
                InputManager.Instance.OnCancel += Cancel;
            }
        }

        public void Hide()
        {
            HasInputFocus = false;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove -= Navigate;
                InputManager.Instance.OnConfirm -= Confirm;
                InputManager.Instance.OnCancel -= Cancel;
            }

            if (_root != null)
                Destroy(_root);

            _optionTexts.Clear();
            _accentBars.Clear();
            _trishulIcons.Clear();
            _onSelect = null;
            _onCancel = null;
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        #region Input handling

        private void Navigate(Vector2Int dir)
        {
            if (dir.y == 0) return;
            int step = dir.y > 0 ? -1 : 1;
            int next = FindFirstEnabled(_selectedIndex + step, step);
            if (next >= 0)
            {
                _selectedIndex = next;
                UpdateSelection();
            }
        }

        private void Confirm()
        {
            if (IsDisabled(_selectedIndex)) return;
            int index = _selectedIndex;
            var callback = _onSelect;
            Hide();
            callback?.Invoke(index);
        }

        private void Cancel()
        {
            var callback = _onCancel;
            Hide();
            callback?.Invoke();
        }

        private bool IsDisabled(int index) =>
            _enabledFlags != null && index < _enabledFlags.Count && !_enabledFlags[index];

        private int FindFirstEnabled(int start, int step)
        {
            if (_options == null || _options.Count == 0) return 0;
            int count = _options.Count;
            int idx = ((start % count) + count) % count;
            for (int i = 0; i < count; i++)
            {
                if (!IsDisabled(idx)) return idx;
                idx = ((idx + step) % count + count) % count;
            }
            return 0;
        }

        #endregion

        #region UI construction

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _optionTexts.Clear();
            _accentBars.Clear();
            _trishulIcons.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            int optCount = _options.Count;
            int dividerCount = Mathf.Max(0, optCount - 1);
            float panelHeight = optCount * OptionHeight + dividerCount * DividerHeight + PanelPaddingY * 2;

            // Root — anchored right-center of canvas, offset left to clear HUD
            _root = new GameObject("ActionMenu");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-40f, 0f);
            rootRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            // Octagonal background (9-sliced sprite with shape baked in)
            var bgImg = _root.AddComponent<Image>();
            bgImg.sprite = _bgSprite;
            bgImg.type = _bgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bgImg.color = _colBgTint;
            bgImg.pixelsPerUnitMultiplier = 1f;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // Options container inside the panel
            var container = new GameObject("Options");
            container.transform.SetParent(_root.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(8f, PanelPaddingY);
            containerRect.offsetMax = new Vector2(-8f, -PanelPaddingY);

            float yOffset = 0f;
            for (int i = 0; i < optCount; i++)
            {
                bool disabled = IsDisabled(i);
                BuildOptionRow(container.transform, i, yOffset, disabled);
                yOffset += OptionHeight;

                if (i < optCount - 1)
                {
                    BuildDivider(container.transform, yOffset);
                    yOffset += DividerHeight;
                }
            }
        }

        private void BuildOptionRow(Transform parent, int index, float yOffset, bool disabled)
        {
            var row = new GameObject(_options[index]);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -yOffset);
            rowRect.sizeDelta = new Vector2(0f, OptionHeight);

            // Left ember accent bar (hidden by default, shown when selected)
            var bar = new GameObject("AccentBar");
            bar.transform.SetParent(row.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.05f);
            barRect.anchorMax = new Vector2(0f, 0.95f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(3f, 0f);
            var barImg = bar.AddComponent<Image>();
            barImg.color = _colAccentBar;
            bar.SetActive(false);
            _accentBars.Add(bar);

            // Trishul cursor icon (hidden by default)
            var trishul = new GameObject("Trishul");
            trishul.transform.SetParent(row.transform, false);
            var tRt = trishul.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(16f, 0f);
            tRt.sizeDelta = new Vector2(TrishulSize, TrishulSize);
            var tImg = trishul.AddComponent<Image>();
            tImg.sprite = _cursorSprite;
            tImg.preserveAspect = true;
            trishul.SetActive(false);
            _trishulIcons.Add(trishul);

            // Option text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(row.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(TextLeftOffset, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = _options[index];
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = disabled ? ColTextDisabled : _colTextDefault;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.characterSpacing = 4f;
            if (_optionFont != null) tmp.font = _optionFont;

            _optionTexts.Add(tmp);
        }

        private void BuildDivider(Transform parent, float yOffset)
        {
            var div = new GameObject("Divider");
            div.transform.SetParent(parent, false);
            var divRect = div.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0f, 1f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.anchoredPosition = new Vector2(0f, -yOffset);
            divRect.sizeDelta = new Vector2(-32f, DividerHeight);

            var img = div.AddComponent<Image>();
            img.sprite = _dividerSprite;
            img.type = _dividerSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = Color.white;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _optionTexts.Count; i++)
            {
                bool selected = i == _selectedIndex;
                bool disabled = IsDisabled(i);

                _optionTexts[i].color = disabled ? ColTextDisabled
                    : selected ? _colTextSelected : _colTextDefault;

                if (selected && !disabled && _selectedGlowMat != null)
                    _optionTexts[i].fontMaterial = _selectedGlowMat;
                else if (_optionFont != null)
                    _optionTexts[i].fontMaterial = _optionFont.material;
                // else: leave material as-is; assigning null to TMP.fontMaterial throws NRE.

                if (i < _accentBars.Count)
                    _accentBars[i].SetActive(selected && !disabled);
                if (i < _trishulIcons.Count)
                    _trishulIcons[i].SetActive(selected && !disabled);
            }
        }

        #endregion
    }
}
