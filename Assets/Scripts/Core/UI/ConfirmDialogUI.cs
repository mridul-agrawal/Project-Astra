using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Reusable Yes/No modal. Built dynamically the first time Show is called so it can
    /// be attached to any host GameObject without requiring a prefab.
    /// </summary>
    public class ConfirmDialogUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.22f, 0.22f, 0.45f, 0.95f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);

        private GameObject _root;
        private TextMeshProUGUI _messageText;
        private readonly List<TextMeshProUGUI> _optionTexts = new();
        private int _selectedIndex;
        private Action _onYes;
        private Action _onNo;

        public void Show(string message, Action onYes, Action onNo)
        {
            _onYes = onYes;
            _onNo = onNo;
            _selectedIndex = 1; // Default to "No" so accidental confirms don't destroy items.

            BuildUI(message);
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

            if (_root != null) Destroy(_root);
            _optionTexts.Clear();
            _onYes = null;
            _onNo = null;
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y == 0 && dir.x == 0) return;
            _selectedIndex = _selectedIndex == 0 ? 1 : 0;
            UpdateSelection();
        }

        private void Confirm()
        {
            int index = _selectedIndex;
            var yes = _onYes;
            var no = _onNo;
            Hide();
            if (index == 0) yes?.Invoke();
            else no?.Invoke();
        }

        private void Cancel()
        {
            var no = _onNo;
            Hide();
            no?.Invoke();
        }

        private void BuildUI(string message)
        {
            if (_root != null) Destroy(_root);
            _optionTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            const float panelWidth = 360f;
            const float panelHeight = 120f;
            const float borderWidth = 3f;

            _root = new GameObject("ConfirmDialog");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(panelWidth + borderWidth * 2, panelHeight + borderWidth * 2);

            var borderImg = _root.AddComponent<Image>();
            borderImg.color = BorderColor;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(borderWidth, borderWidth);
            panelRect.offsetMax = new Vector2(-borderWidth, -borderWidth);
            panel.AddComponent<Image>().color = PanelColor;

            var msgGo = new GameObject("Message");
            msgGo.transform.SetParent(panel.transform, false);
            var msgRect = msgGo.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0f, 0.45f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.offsetMin = new Vector2(12f, 0f);
            msgRect.offsetMax = new Vector2(-12f, -8f);

            _messageText = msgGo.AddComponent<TextMeshProUGUI>();
            _messageText.text = message;
            _messageText.fontSize = 18;
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = TextNormal;
            _messageText.fontStyle = FontStyles.Bold;
            _messageText.enableWordWrapping = true;

            string[] labels = { "Yes", "No" };
            for (int i = 0; i < 2; i++)
            {
                var optGo = new GameObject(labels[i]);
                optGo.transform.SetParent(panel.transform, false);
                var optRect = optGo.AddComponent<RectTransform>();
                optRect.anchorMin = new Vector2(i == 0 ? 0.15f : 0.55f, 0.05f);
                optRect.anchorMax = new Vector2(i == 0 ? 0.45f : 0.85f, 0.4f);
                optRect.offsetMin = Vector2.zero;
                optRect.offsetMax = Vector2.zero;

                var tmp = optGo.AddComponent<TextMeshProUGUI>();
                tmp.text = labels[i];
                tmp.fontSize = 22;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = TextNormal;
                _optionTexts.Add(tmp);
            }

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _optionTexts.Count; i++)
                _optionTexts[i].color = i == _selectedIndex ? TextSelected : TextNormal;
        }
    }
}
