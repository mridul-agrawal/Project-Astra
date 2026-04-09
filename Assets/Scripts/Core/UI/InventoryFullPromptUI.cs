using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Modal that appears when a unit tries to acquire an item with a full inventory.
    /// Shows the incoming item plus all 5 current items so the player can pick which
    /// slot to discard, or cancel. Implements IInventoryFullPromptHandler so the
    /// static InventoryAcquisition flow can defer UI to it.
    /// </summary>
    public class InventoryFullPromptUI : MonoBehaviour, IInventoryFullPromptHandler
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.18f, 0.18f, 0.4f, 0.95f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);
        private static readonly Color TextHighlight = new(1f, 0.85f, 0.5f, 1f);

        const float OptionHeight = 28f;
        const float PanelPadding = 12f;
        const float BorderWidth = 3f;
        const float PanelWidth = 360f;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _slotTexts = new();
        private TextMeshProUGUI _cursorIndicator;
        private int _selectedIndex;
        private TestUnit _unit;
        private InventoryItem _incoming;
        private Action<int> _onChooseDiscard;
        private Action _onCancel;

        public void Prompt(TestUnit unit, InventoryItem incoming,
            Action<int> onChooseDiscardSlot, Action onCancel)
        {
            _unit = unit;
            _incoming = incoming;
            _onChooseDiscard = onChooseDiscardSlot;
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

            if (_root != null) Destroy(_root);
            _slotTexts.Clear();
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0)
                _selectedIndex = _selectedIndex <= 0 ? UnitInventory.Capacity - 1 : _selectedIndex - 1;
            else if (dir.y < 0)
                _selectedIndex = _selectedIndex >= UnitInventory.Capacity - 1 ? 0 : _selectedIndex + 1;
            else
                return;

            UpdateSelection();
        }

        private void Confirm()
        {
            int index = _selectedIndex;
            var cb = _onChooseDiscard;
            Hide();
            cb?.Invoke(index);
        }

        private void Cancel()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _slotTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float panelHeight = (UnitInventory.Capacity + 3) * OptionHeight + PanelPadding * 2;

            _root = new GameObject("InventoryFullPrompt");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(PanelWidth + BorderWidth * 2, panelHeight + BorderWidth * 2);

            _root.AddComponent<Image>().color = BorderColor;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(BorderWidth, BorderWidth);
            panelRect.offsetMax = new Vector2(-BorderWidth, -BorderWidth);
            panel.AddComponent<Image>().color = PanelColor;

            float y = -PanelPadding;
            y -= AddRow(panel, y, "Inventory Full — Choose item to discard:", TextHighlight);
            y -= AddRow(panel, y, $"Incoming: {FormatItem(_incoming)}", TextHighlight);
            y -= AddRow(panel, y, "─────────────────────", TextNormal);

            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                var slot = _unit.Inventory.GetSlot(i);
                var rowGo = new GameObject($"Slot{i}");
                rowGo.transform.SetParent(panel.transform, false);
                var rowRect = rowGo.AddComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(PanelPadding + 20f, y);
                rowRect.sizeDelta = new Vector2(-PanelPadding * 2 - 20f, OptionHeight);

                var tmp = rowGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{i + 1}. {FormatItem(slot)}";
                tmp.fontSize = 16;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = TextNormal;
                tmp.enableWordWrapping = false;
                _slotTexts.Add(tmp);

                y -= OptionHeight;
            }

            // Cursor indicator
            var cursorGo = new GameObject("Cursor");
            cursorGo.transform.SetParent(panel.transform, false);
            var cursorRect = cursorGo.AddComponent<RectTransform>();
            cursorRect.anchorMin = new Vector2(0f, 1f);
            cursorRect.anchorMax = new Vector2(0f, 1f);
            cursorRect.pivot = new Vector2(0f, 0.5f);
            cursorRect.sizeDelta = new Vector2(20f, OptionHeight);
            cursorRect.anchoredPosition = new Vector2(PanelPadding, -PanelPadding - 3 * OptionHeight - OptionHeight * 0.5f);

            _cursorIndicator = cursorGo.AddComponent<TextMeshProUGUI>();
            _cursorIndicator.text = "\u25B6";
            _cursorIndicator.fontSize = 16;
            _cursorIndicator.alignment = TextAlignmentOptions.Center;
            _cursorIndicator.color = TextSelected;
            _cursorIndicator.enableWordWrapping = false;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private float AddRow(GameObject parent, float y, string text, Color color)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(PanelPadding, y);
            rect.sizeDelta = new Vector2(-PanelPadding * 2, OptionHeight);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            return OptionHeight;
        }

        private static string FormatItem(InventoryItem item)
        {
            if (item.IsEmpty) return "—";

            if (item.kind == ItemKind.Weapon)
            {
                var w = item.weapon;
                string uses = w.indestructible ? "∞" : $"{w.currentUses}/{w.maxUses}";
                return $"{w.name}  Mt {w.might}  Hit {w.hit}  ({uses})";
            }

            if (item.kind == ItemKind.Consumable)
            {
                var c = item.consumable;
                return $"{c.name}  +{c.magnitude}  ({c.currentUses}/{c.maxUses})";
            }

            return item.DisplayName;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _slotTexts.Count; i++)
                _slotTexts[i].color = i == _selectedIndex ? TextSelected : TextNormal;

            if (_cursorIndicator != null)
            {
                var rect = _cursorIndicator.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    PanelPadding,
                    -PanelPadding - 3 * OptionHeight - _selectedIndex * OptionHeight - OptionHeight * 0.5f);
            }
        }
    }
}
