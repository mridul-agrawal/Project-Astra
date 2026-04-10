using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Side-by-side trade screen for two units. Supports swap, give, and take operations
    /// with a two-phase selection model (Browsing → ItemSelected → execute → Browsing).
    /// Cancel while browsing shows a confirm dialog if changes were made.
    /// </summary>
    public class TradeUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.18f, 0.18f, 0.4f, 0.92f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);
        private static readonly Color TextEmpty = new(0.55f, 0.55f, 0.6f, 1f);
        private static readonly Color TextHighlighted = new(0.5f, 1f, 0.8f, 1f);
        private static readonly Color TextBroken = new(0.85f, 0.4f, 0.4f, 1f);

        const float SlotHeight = 28f;
        const float PanelPadding = 12f;
        const float BorderWidth = 3f;
        const float ColumnWidth = 260f;
        const float ColumnGap = 16f;

        private enum Column { Left, Right }
        private enum Phase { Browsing, ItemSelected }

        private TradeSession _session;
        private ConfirmDialogUI _confirmDialog;
        private Action _onConfirm;
        private Action _onCancel;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _leftTexts = new();
        private readonly List<TextMeshProUGUI> _rightTexts = new();
        private TextMeshProUGUI _cursorIndicator;
        private TextMeshProUGUI _footerText;

        private Column _activeColumn;
        private int _cursorRow;
        private Phase _phase;
        private Column _selectedColumn;
        private int _selectedRow;

        public void Show(TradeSession session, ConfirmDialogUI confirmDialog,
            Action onConfirm, Action onCancel)
        {
            _session = session;
            _confirmDialog = confirmDialog;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _activeColumn = Column.Left;
            _cursorRow = 0;
            _phase = Phase.Browsing;

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
            _leftTexts.Clear();
            _rightTexts.Clear();
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
            if (dir.y > 0)
                _cursorRow = _cursorRow <= 0 ? TradeSession.Capacity - 1 : _cursorRow - 1;
            else if (dir.y < 0)
                _cursorRow = _cursorRow >= TradeSession.Capacity - 1 ? 0 : _cursorRow + 1;
            else if (dir.x != 0)
                _activeColumn = _activeColumn == Column.Left ? Column.Right : Column.Left;
            else
                return;

            UpdateVisuals();
        }

        private void Confirm()
        {
            if (_phase == Phase.Browsing)
            {
                var item = GetSlotAtCursor();
                if (item.IsEmpty) return;

                _phase = Phase.ItemSelected;
                _selectedColumn = _activeColumn;
                _selectedRow = _cursorRow;
                UpdateVisuals();
                return;
            }

            if (_phase == Phase.ItemSelected)
            {
                if (_activeColumn == _selectedColumn)
                {
                    Deselect();
                    return;
                }

                bool success = ExecuteTradeOperation();
                Deselect();
                if (success) RefreshAllLabels();
                UpdateVisuals();
            }
        }

        private void Cancel()
        {
            if (_phase == Phase.ItemSelected)
            {
                Deselect();
                return;
            }

            if (!_session.HasChanges)
            {
                var cancel = _onCancel;
                Hide();
                cancel?.Invoke();
                return;
            }

            // Pause input while dialog is active.
            HasInputFocus = false;
            UnsubscribeInput();

            _confirmDialog.Show("Apply trade changes?",
                onYes: () =>
                {
                    _session.Commit();
                    var confirm = _onConfirm;
                    Hide();
                    confirm?.Invoke();
                },
                onNo: () =>
                {
                    var cancel = _onCancel;
                    Hide();
                    cancel?.Invoke();
                });
        }

        private void Deselect()
        {
            _phase = Phase.Browsing;
            UpdateVisuals();
        }

        #endregion

        #region Trade operations

        private bool ExecuteTradeOperation()
        {
            if (_selectedColumn == Column.Left && _activeColumn == Column.Right)
            {
                var rightItem = _session.GetRightSlot(_cursorRow);
                if (rightItem.IsEmpty)
                    return _session.TryGive(_selectedRow);
                else
                    return _session.TrySwap(_selectedRow, _cursorRow);
            }

            if (_selectedColumn == Column.Right && _activeColumn == Column.Left)
            {
                var leftItem = _session.GetLeftSlot(_cursorRow);
                if (leftItem.IsEmpty)
                    return _session.TryTake(_selectedRow);
                else
                    return _session.TrySwap(_cursorRow, _selectedRow);
            }

            return false;
        }

        private InventoryItem GetSlotAtCursor()
        {
            return _activeColumn == Column.Left
                ? _session.GetLeftSlot(_cursorRow)
                : _session.GetRightSlot(_cursorRow);
        }

        #endregion

        #region UI construction

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _leftTexts.Clear();
            _rightTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float totalWidth = ColumnWidth * 2 + ColumnGap + PanelPadding * 2;
            float totalHeight = (TradeSession.Capacity + 2) * SlotHeight + PanelPadding * 2;

            _root = new GameObject("TradeMenu");
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

            BuildColumnHeader(panel, leftX, headerY, _session.LeftUnit.name);
            BuildColumnHeader(panel, rightX, headerY, _session.RightUnit.name);

            float slotsStartY = headerY - SlotHeight;
            BuildSlotColumn(panel, leftX + 20f, slotsStartY, _leftTexts);
            BuildSlotColumn(panel, rightX + 20f, slotsStartY, _rightTexts);

            // Cursor indicator
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

            // Footer hint
            float footerY = slotsStartY - TradeSession.Capacity * SlotHeight - 4f;
            var footerGo = CreateChild(panel, "Footer");
            var footerRect = footerGo.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 1f);
            footerRect.anchorMax = new Vector2(1f, 1f);
            footerRect.pivot = new Vector2(0f, 1f);
            footerRect.anchoredPosition = new Vector2(PanelPadding, footerY);
            footerRect.sizeDelta = new Vector2(-PanelPadding * 2, SlotHeight);

            _footerText = footerGo.AddComponent<TextMeshProUGUI>();
            _footerText.text = "Select item, then target slot | B: Exit";
            _footerText.fontSize = 14;
            _footerText.fontStyle = FontStyles.Italic;
            _footerText.alignment = TextAlignmentOptions.MidlineLeft;
            _footerText.color = TextEmpty;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void BuildColumnHeader(GameObject parent, float x, float y, string label)
        {
            var go = CreateChild(parent, $"Header_{label}");
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(ColumnWidth, SlotHeight);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(1f, 0.85f, 0.5f, 1f);
        }

        private void BuildSlotColumn(GameObject parent, float x, float startY,
            List<TextMeshProUGUI> textList)
        {
            for (int i = 0; i < TradeSession.Capacity; i++)
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
            for (int i = 0; i < TradeSession.Capacity; i++)
            {
                _leftTexts[i].text = FormatSlot(i, _session.GetLeftSlot(i));
                _rightTexts[i].text = FormatSlot(i, _session.GetRightSlot(i));
            }
        }

        private void UpdateVisuals()
        {
            float leftX = PanelPadding;
            float rightX = PanelPadding + ColumnWidth + ColumnGap;
            float slotsStartY = -PanelPadding - SlotHeight;

            for (int i = 0; i < TradeSession.Capacity; i++)
            {
                _leftTexts[i].color = GetSlotColor(Column.Left, i, _session.GetLeftSlot(i));
                _rightTexts[i].color = GetSlotColor(Column.Right, i, _session.GetRightSlot(i));
            }

            if (_cursorIndicator != null)
            {
                float cursorX = _activeColumn == Column.Left ? leftX : rightX;
                float cursorY = slotsStartY - _cursorRow * SlotHeight - SlotHeight * 0.5f;
                var rect = _cursorIndicator.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(cursorX, cursorY);
            }

            if (_footerText != null)
            {
                _footerText.text = _phase == Phase.ItemSelected
                    ? "Select target slot to swap/give/take | B: Deselect"
                    : "Select item, then target slot | B: Exit";
            }
        }

        private Color GetSlotColor(Column col, int row, InventoryItem item)
        {
            bool isSelected = _phase == Phase.ItemSelected && col == _selectedColumn && row == _selectedRow;
            if (isSelected) return TextHighlighted;

            bool isCursor = col == _activeColumn && row == _cursorRow;
            if (isCursor) return TextSelected;

            if (item.IsEmpty) return TextEmpty;
            if (item.IsDepleted) return TextBroken;
            return TextNormal;
        }

        private static string FormatSlot(int index, InventoryItem item)
        {
            if (item.IsEmpty) return $" {index + 1}. \u2014";

            string uses = item.Indestructible
                ? "\u221E"
                : $"{item.CurrentUses}/{item.MaxUses}";
            return $" {index + 1}. {item.DisplayName}  ({uses})";
        }

        #endregion
    }
}
