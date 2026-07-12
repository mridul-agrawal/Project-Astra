using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.UI.Overlays
{
    // Reusable Yes/No modal. Built dynamically the first time Show is called
    // so it can be attached to any host GameObject without requiring a prefab.
    public class ConfirmDialogUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.22f, 0.22f, 0.45f, 0.95f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);

        private GameObject root;
        private TextMeshProUGUI messageText;
        private readonly List<TextMeshProUGUI> optionTexts = new();
        private int selectedIndex;
        private Action onYes;
        private Action onNo;

        public void Show(string message, Action onYes, Action onNo)
        {
            this.onYes = onYes;
            this.onNo = onNo;
            selectedIndex = 1; // Default to "No" so accidental confirms don't destroy items.

            BuildUI(message);
            UpdateSelection();

            HasInputFocus = true;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += Navigate;
                InputManager.Instance.OnConfirm += Confirm;
                InputManager.Instance.OnCancel += Cancel;
            }

            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
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

            if (root != null) Destroy(root);
            optionTexts.Clear();
            onYes = null;
            onNo = null;
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y == 0 && dir.x == 0) return;
            selectedIndex = selectedIndex == 0 ? 1 : 0;
            UpdateSelection();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void Confirm()
        {
            AudioManager.Instance?.Play(SoundId.UiConfirm);
            int index = selectedIndex;
            var yes = onYes;
            var no = onNo;
            Hide();
            if (index == 0) yes?.Invoke();
            else no?.Invoke();
        }

        private void Cancel()
        {
            AudioManager.Instance?.Play(SoundId.UiCancel);
            var no = onNo;
            Hide();
            no?.Invoke();
        }

        private void BuildUI(string message)
        {
            if (root != null) Destroy(root);
            optionTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            const float panelWidth = 360f;
            const float panelHeight = 120f;
            const float borderWidth = 3f;

            root = new GameObject("ConfirmDialog");
            root.transform.SetParent(canvas.transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(panelWidth + borderWidth * 2, panelHeight + borderWidth * 2);

            var borderImg = root.AddComponent<Image>();
            borderImg.color = BorderColor;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
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

            messageText = msgGo.AddComponent<TextMeshProUGUI>();
            messageText.text = message;
            messageText.fontSize = 18;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = TextNormal;
            messageText.fontStyle = FontStyles.Bold;
            messageText.enableWordWrapping = true;

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
                optionTexts.Add(tmp);
            }

            root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < optionTexts.Count; i++)
                optionTexts[i].color = i == selectedIndex ? TextSelected : TextNormal;
        }
    }
}
