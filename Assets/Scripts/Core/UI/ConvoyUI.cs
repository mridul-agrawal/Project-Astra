using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Two-panel convoy interface: scrollable convoy list (left) and lord inventory (right).
    /// Withdraw moves items from convoy to lord, Store moves items from lord to convoy.
    /// Supply consumes the lord's action — onClose calls CompleteAction.
    /// </summary>
    public class ConvoyUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.18f, 0.18f, 0.4f, 0.92f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);
        private static readonly Color TextEmpty = new(0.55f, 0.55f, 0.6f, 1f);

        const float SlotHeight = 28f;
        const float PanelPadding = 12f;
        const float BorderWidth = 3f;
        const float ColumnWidth = 280f;
        const float ColumnGap = 16f;
        const int VisibleConvoyRows = 8;

        private enum Panel { Convoy, Unit }

        private SupplyConvoy _convoy;
        private TestUnit _lordUnit;
        private ToastNotificationUI _toastUI;
        private Action _onClose;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _convoyTexts = new();
        private readonly List<TextMeshProUGUI> _unitTexts = new();
        private TextMeshProUGUI _cursorIndicator;
        private TextMeshProUGUI _headerLeft;
        private TextMeshProUGUI _footerText;
        private TextMeshProUGUI _scrollUpIndicator;
        private TextMeshProUGUI _scrollDownIndicator;

        private Panel _activePanel;
        private int _cursorRow;
        private int _scrollOffset;

        public void Show(SupplyConvoy convoy, TestUnit lordUnit,
            ToastNotificationUI toastUI, Action onClose)
        {
            _convoy = convoy;
            _lordUnit = lordUnit;
            _toastUI = toastUI;
            _onClose = onClose;
            _activePanel = Panel.Convoy;
            _cursorRow = 0;
            _scrollOffset = 0;

            BuildUI();
            RefreshAllLabels();
            UpdateVisuals();

            HasInputFocus = true;
            SubscribeInput();
        }

        public void Hide()
        {
            HasInputFocus = false;
            UnsubscribeInput();
            if (_root != null) Destroy(_root);
            _convoyTexts.Clear();
            _unitTexts.Clear();
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        #region Input

        private void SubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += Confirm;
            InputManager.Instance.OnCancel += Cancel;
        }

        private void UnsubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= Confirm;
            InputManager.Instance.OnCancel -= Cancel;
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.x != 0)
            {
                _activePanel = _activePanel == Panel.Convoy ? Panel.Unit : Panel.Convoy;
                ClampCursor();
                UpdateVisuals();
                return;
            }

            if (dir.y == 0) return;

            if (_activePanel == Panel.Convoy)
                NavigateConvoyPanel(dir.y > 0 ? -1 : 1);
            else
                NavigateUnitPanel(dir.y > 0 ? -1 : 1);

            UpdateVisuals();
        }

        private void NavigateConvoyPanel(int step)
        {
            int count = _convoy.Count;
            if (count == 0) return;

            int absIndex = _scrollOffset + _cursorRow + step;

            if (absIndex < 0) absIndex = 0;
            else if (absIndex >= count) absIndex = count - 1;

            if (absIndex < _scrollOffset)
                _scrollOffset = absIndex;
            else if (absIndex >= _scrollOffset + VisibleConvoyRows)
                _scrollOffset = absIndex - VisibleConvoyRows + 1;

            _cursorRow = absIndex - _scrollOffset;
            RefreshConvoyLabels();
        }

        private void NavigateUnitPanel(int step)
        {
            int max = UnitInventory.Capacity;
            _cursorRow += step;
            if (_cursorRow < 0) _cursorRow = max - 1;
            else if (_cursorRow >= max) _cursorRow = 0;
        }

        private void ClampCursor()
        {
            if (_activePanel == Panel.Convoy)
            {
                int maxRow = Mathf.Min(VisibleConvoyRows, _convoy.Count) - 1;
                if (_cursorRow > maxRow) _cursorRow = Mathf.Max(0, maxRow);
            }
            else
            {
                if (_cursorRow >= UnitInventory.Capacity) _cursorRow = UnitInventory.Capacity - 1;
            }
        }

        private void Confirm()
        {
            if (_activePanel == Panel.Convoy)
                TryWithdraw();
            else
                TryStore();
        }

        private void Cancel()
        {
            var close = _onClose;
            Hide();
            close?.Invoke();
        }

        #endregion

        #region Withdraw / Store

        private void TryWithdraw()
        {
            int absIndex = _scrollOffset + _cursorRow;
            if (absIndex >= _convoy.Count) return;

            var item = _convoy.GetSlot(absIndex);
            if (item.IsEmpty) return;

            var inventory = _lordUnit.Inventory;
            if (inventory.IsFull)
            {
                _toastUI?.Show("Inventory full");
                return;
            }

            if (!_convoy.TryWithdraw(absIndex, out var withdrawn)) return;
            inventory.TryAddItem(withdrawn, out _);

            ClampAfterConvoyChange();
            RefreshAllLabels();
            UpdateVisuals();
        }

        private void TryStore()
        {
            var inventory = _lordUnit.Inventory;
            var item = inventory.GetSlot(_cursorRow);
            if (item.IsEmpty) return;

            if (_convoy.IsFull)
            {
                _toastUI?.Show("Convoy full");
                return;
            }

            inventory.DiscardSlot(_cursorRow);
            _convoy.TryDeposit(item);

            RefreshAllLabels();
            UpdateVisuals();
        }

        private void ClampAfterConvoyChange()
        {
            int maxScroll = Mathf.Max(0, _convoy.Count - VisibleConvoyRows);
            if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
            ClampCursor();
        }

        #endregion

        #region UI construction

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _convoyTexts.Clear();
            _unitTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float totalWidth = ColumnWidth * 2 + ColumnGap + PanelPadding * 2;
            float totalHeight = (VisibleConvoyRows + 3) * SlotHeight + PanelPadding * 2;

            _root = new GameObject("ConvoyMenu");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(totalWidth + BorderWidth * 2, totalHeight + BorderWidth * 2);

            _root.AddComponent<Image>().color = BorderColor;

            var panel = CreateChild(_root, "Panel");
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(BorderWidth, BorderWidth);
            panelRect.offsetMax = new Vector2(-BorderWidth, -BorderWidth);
            panel.AddComponent<Image>().color = PanelColor;

            float leftX = PanelPadding;
            float rightX = PanelPadding + ColumnWidth + ColumnGap;
            float headerY = -PanelPadding;

            _headerLeft = BuildLabel(panel, leftX, headerY, ColumnWidth, "", 18, FontStyles.Bold,
                new Color(1f, 0.85f, 0.5f, 1f));
            BuildLabel(panel, rightX, headerY, ColumnWidth,
                $"{_lordUnit.name} \u2014 Items", 18, FontStyles.Bold, new Color(1f, 0.85f, 0.5f, 1f));

            float slotsStartY = headerY - SlotHeight;

            // Scroll-up indicator
            _scrollUpIndicator = BuildLabel(panel, leftX + 20f, slotsStartY, ColumnWidth - 20f,
                "\u25B2 more", 14, FontStyles.Italic, TextEmpty);

            float convoyRowsStartY = slotsStartY - SlotHeight;
            BuildSlotColumn(panel, leftX + 20f, convoyRowsStartY, VisibleConvoyRows, _convoyTexts);
            BuildSlotColumn(panel, rightX + 20f, slotsStartY, UnitInventory.Capacity, _unitTexts);

            // Scroll-down indicator
            float scrollDownY = convoyRowsStartY - VisibleConvoyRows * SlotHeight;
            _scrollDownIndicator = BuildLabel(panel, leftX + 20f, scrollDownY, ColumnWidth - 20f,
                "\u25BC more", 14, FontStyles.Italic, TextEmpty);

            // Cursor
            var cursorGo = CreateChild(panel, "Cursor");
            var cursorRect = cursorGo.AddComponent<RectTransform>();
            cursorRect.anchorMin = new Vector2(0f, 1f);
            cursorRect.anchorMax = new Vector2(0f, 1f);
            cursorRect.pivot = new Vector2(0f, 0.5f);
            cursorRect.sizeDelta = new Vector2(20f, SlotHeight);

            _cursorIndicator = cursorGo.AddComponent<TextMeshProUGUI>();
            _cursorIndicator.text = "\u25B6";
            _cursorIndicator.fontSize = 16;
            _cursorIndicator.alignment = TextAlignmentOptions.Center;
            _cursorIndicator.color = TextSelected;

            // Footer
            float footerY = Mathf.Min(scrollDownY, slotsStartY - UnitInventory.Capacity * SlotHeight) - SlotHeight;
            var footerGo = CreateChild(panel, "Footer");
            var footerRect = footerGo.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 1f);
            footerRect.anchorMax = new Vector2(1f, 1f);
            footerRect.pivot = new Vector2(0f, 1f);
            footerRect.anchoredPosition = new Vector2(PanelPadding, footerY);
            footerRect.sizeDelta = new Vector2(-PanelPadding * 2, SlotHeight);

            _footerText = footerGo.AddComponent<TextMeshProUGUI>();
            _footerText.fontSize = 14;
            _footerText.fontStyle = FontStyles.Italic;
            _footerText.alignment = TextAlignmentOptions.MidlineLeft;
            _footerText.color = TextEmpty;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void BuildSlotColumn(GameObject parent, float x, float startY, int rowCount,
            List<TextMeshProUGUI> textList)
        {
            for (int i = 0; i < rowCount; i++)
            {
                var go = CreateChild(parent, $"Slot{i}");
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, startY - i * SlotHeight);
                rect.sizeDelta = new Vector2(ColumnWidth - 20f, SlotHeight);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = TextNormal;
                textList.Add(tmp);
            }
        }

        private static TextMeshProUGUI BuildLabel(GameObject parent, float x, float y,
            float width, string text, int fontSize, FontStyles style, Color color)
        {
            var go = CreateChild(parent, "Label");
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, SlotHeight);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = color;
            return tmp;
        }

        private static GameObject CreateChild(GameObject parent, string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        #endregion

        #region Visual updates

        private void RefreshAllLabels()
        {
            RefreshConvoyLabels();
            RefreshUnitLabels();
            if (_headerLeft != null)
                _headerLeft.text = $"Supply Convoy ({_convoy.Count}/{SupplyConvoy.MaxCapacity})";
        }

        private void RefreshConvoyLabels()
        {
            for (int i = 0; i < _convoyTexts.Count; i++)
            {
                int absIndex = _scrollOffset + i;
                var item = _convoy.GetSlot(absIndex);
                _convoyTexts[i].text = item.IsEmpty ? "" : FormatItem(item);
            }

            if (_scrollUpIndicator != null)
                _scrollUpIndicator.color = _scrollOffset > 0 ? TextEmpty : Color.clear;
            if (_scrollDownIndicator != null)
                _scrollDownIndicator.color = _scrollOffset + VisibleConvoyRows < _convoy.Count
                    ? TextEmpty : Color.clear;
        }

        private void RefreshUnitLabels()
        {
            var inv = _lordUnit.Inventory;
            int equipped = inv.EquippedWeaponSlot;
            for (int i = 0; i < _unitTexts.Count; i++)
            {
                var item = inv.GetSlot(i);
                string mark = i == equipped ? "\u2605 " : "  ";
                _unitTexts[i].text = item.IsEmpty
                    ? $"  {i + 1}. \u2014"
                    : $"{mark}{i + 1}. {FormatItem(item)}";
            }
        }

        private void UpdateVisuals()
        {
            float leftX = PanelPadding;
            float rightX = PanelPadding + ColumnWidth + ColumnGap;
            float slotsStartY = -PanelPadding - SlotHeight;
            float convoyRowsStartY = slotsStartY - SlotHeight;

            // Color convoy rows
            for (int i = 0; i < _convoyTexts.Count; i++)
            {
                int absIndex = _scrollOffset + i;
                var item = _convoy.GetSlot(absIndex);
                bool isCursor = _activePanel == Panel.Convoy && i == _cursorRow;
                _convoyTexts[i].color = isCursor ? TextSelected : (item.IsEmpty ? TextEmpty : TextNormal);
            }

            // Color unit rows
            for (int i = 0; i < _unitTexts.Count; i++)
            {
                var item = _lordUnit.Inventory.GetSlot(i);
                bool isCursor = _activePanel == Panel.Unit && i == _cursorRow;
                _unitTexts[i].color = isCursor ? TextSelected : (item.IsEmpty ? TextEmpty : TextNormal);
            }

            // Position cursor indicator
            if (_cursorIndicator != null)
            {
                float cursorX = _activePanel == Panel.Convoy ? leftX : rightX;
                float rowStartY = _activePanel == Panel.Convoy ? convoyRowsStartY : slotsStartY;
                float cursorY = rowStartY - _cursorRow * SlotHeight - SlotHeight * 0.5f;
                var rect = _cursorIndicator.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(cursorX, cursorY);
            }

            // Footer context
            if (_footerText != null)
            {
                _footerText.text = _activePanel == Panel.Convoy
                    ? "Confirm: Withdraw | L/R: Switch | B: Close"
                    : "Confirm: Store | L/R: Switch | B: Close";
            }
        }

        private static string FormatItem(InventoryItem item)
        {
            if (item.IsEmpty) return "\u2014";
            string uses = item.Indestructible ? "\u221E" : $"{item.CurrentUses}/{item.MaxUses}";
            return $"{item.DisplayName}  ({uses})";
        }

        #endregion
    }
}
