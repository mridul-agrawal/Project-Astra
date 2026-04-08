using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    public class UnitActionMenuUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.22f, 0.22f, 0.45f, 0.88f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);

        const float OptionHeight = 32f;
        const float PanelPadding = 12f;
        const float BorderWidth = 3f;
        const float CursorOffset = 8f;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _optionTexts = new();
        private TextMeshProUGUI _cursorIndicator;
        private int _selectedIndex;
        private List<string> _options;
        private Action<int> _onSelect;
        private Action _onCancel;

        public void Show(List<string> options, Action<int> onSelect, Action onCancel)
        {
            _options = options;
            _onSelect = onSelect;
            _onCancel = onCancel;
            _selectedIndex = 0;

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
            if (dir.y > 0)
                _selectedIndex = _selectedIndex <= 0 ? _options.Count - 1 : _selectedIndex - 1;
            else if (dir.y < 0)
                _selectedIndex = _selectedIndex >= _options.Count - 1 ? 0 : _selectedIndex + 1;
            else
                return;

            UpdateSelection();
        }

        private void Confirm()
        {
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

        #endregion

        #region UI construction

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _optionTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float panelHeight = _options.Count * OptionHeight + PanelPadding * 2;
            float panelWidth = 180f;

            // Root with border
            _root = new GameObject("ActionMenu");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-20f, 40f);
            rootRect.sizeDelta = new Vector2(panelWidth + BorderWidth * 2, panelHeight + BorderWidth * 2);

            var borderImg = _root.AddComponent<Image>();
            borderImg.color = BorderColor;

            // Inner panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(BorderWidth, BorderWidth);
            panelRect.offsetMax = new Vector2(-BorderWidth, -BorderWidth);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = PanelColor;

            // Options container
            var container = new GameObject("Options");
            container.transform.SetParent(panel.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(PanelPadding + 20f, PanelPadding);
            containerRect.offsetMax = new Vector2(-PanelPadding, -PanelPadding);

            for (int i = 0; i < _options.Count; i++)
            {
                var optGo = new GameObject(_options[i]);
                optGo.transform.SetParent(container.transform, false);
                var optRect = optGo.AddComponent<RectTransform>();
                optRect.anchorMin = new Vector2(0f, 1f);
                optRect.anchorMax = new Vector2(1f, 1f);
                optRect.pivot = new Vector2(0f, 1f);
                optRect.anchoredPosition = new Vector2(0f, -i * OptionHeight);
                optRect.sizeDelta = new Vector2(0f, OptionHeight);

                var tmp = optGo.AddComponent<TextMeshProUGUI>();
                tmp.text = _options[i];
                tmp.fontSize = 20;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = TextNormal;
                tmp.enableWordWrapping = false;

                _optionTexts.Add(tmp);
            }

            // Selection cursor (arrow indicator)
            var cursorGo = new GameObject("Cursor");
            cursorGo.transform.SetParent(container.transform, false);
            var cursorRect = cursorGo.AddComponent<RectTransform>();
            cursorRect.anchorMin = new Vector2(0f, 1f);
            cursorRect.anchorMax = new Vector2(0f, 1f);
            cursorRect.pivot = new Vector2(1f, 0.5f);
            cursorRect.sizeDelta = new Vector2(20f, OptionHeight);
            cursorRect.anchoredPosition = new Vector2(-CursorOffset, -OptionHeight * 0.5f);

            _cursorIndicator = cursorGo.AddComponent<TextMeshProUGUI>();
            _cursorIndicator.text = "\u25B6";
            _cursorIndicator.fontSize = 16;
            _cursorIndicator.alignment = TextAlignmentOptions.Center;
            _cursorIndicator.color = TextSelected;
            _cursorIndicator.enableWordWrapping = false;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _optionTexts.Count; i++)
                _optionTexts[i].color = i == _selectedIndex ? TextSelected : TextNormal;

            if (_cursorIndicator != null)
            {
                var rect = _cursorIndicator.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-CursorOffset, -_selectedIndex * OptionHeight - OptionHeight * 0.5f);
            }
        }

        #endregion
    }
}
