using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Lists a unit's 5 inventory slots with item name and uses, and routes selection
    /// into a slot sub-menu (Equip / Use / Discard). Built dynamically; navigation
    /// follows the same Up/Down/Confirm/Cancel pattern as UnitActionMenuUI.
    /// </summary>
    public class InventoryMenuUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        private static readonly Color PanelColor = new(0.18f, 0.18f, 0.4f, 0.92f);
        private static readonly Color BorderColor = new(0.55f, 0.45f, 0.3f, 1f);
        private static readonly Color TextNormal = Color.white;
        private static readonly Color TextSelected = new(1f, 1f, 0.6f, 1f);
        private static readonly Color TextEmpty = new(0.55f, 0.55f, 0.6f, 1f);
        private static readonly Color TextBroken = new(0.85f, 0.4f, 0.4f, 1f);

        const float OptionHeight = 28f;
        const float PanelPadding = 12f;
        const float BorderWidth = 3f;
        const float PanelWidth = 280f;

        private UnitInventory _inventory;
        private TestUnit _unit;
        private ConfirmDialogUI _confirmDialog;
        private UnitActionMenuUI _slotSubMenu;
        private Action _onConsumableUsed;
        private Action _onClose;

        private GameObject _root;
        private readonly List<TextMeshProUGUI> _slotTexts = new();
        private TextMeshProUGUI _cursorIndicator;
        private int _selectedIndex;
        private bool _slotSubMenuOpen;

        public void Show(TestUnit unit, ConfirmDialogUI confirmDialog,
            Action onConsumableUsed, Action onClose)
        {
            _unit = unit;
            _inventory = unit?.Inventory;
            _confirmDialog = confirmDialog;
            _onConsumableUsed = onConsumableUsed;
            _onClose = onClose;
            _selectedIndex = 0;
            _slotSubMenuOpen = false;

            EnsureSlotSubMenu();
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
            if (_slotSubMenu != null) Destroy(_slotSubMenu.gameObject);
        }

        private void EnsureSlotSubMenu()
        {
            if (_slotSubMenu != null) return;
            var go = new GameObject("InventorySlotSubMenu");
            go.transform.SetParent(transform, false);
            _slotSubMenu = go.AddComponent<UnitActionMenuUI>();
        }

        #region Input handling

        private void Navigate(Vector2Int dir)
        {
            if (_slotSubMenuOpen) return;
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
            if (_slotSubMenuOpen) return;
            var slot = _inventory.GetSlot(_selectedIndex);
            if (slot.IsEmpty) return;
            OpenSlotSubMenu(_selectedIndex, slot);
        }

        private void Cancel()
        {
            if (_slotSubMenuOpen) return;
            var close = _onClose;
            Hide();
            close?.Invoke();
        }

        #endregion

        private void OpenSlotSubMenu(int slotIndex, InventoryItem item)
        {
            _slotSubMenuOpen = true;

            // Temporarily release main menu input so the sub-menu owns it.
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove -= Navigate;
                InputManager.Instance.OnConfirm -= Confirm;
                InputManager.Instance.OnCancel -= Cancel;
            }
            HasInputFocus = false;

            var actions = new List<string>();
            var handlers = new List<Action>();

            if (item.kind == ItemKind.Weapon)
            {
                if (slotIndex != 0 && EquipResolver.CanEquip(_unit, item.weapon))
                {
                    actions.Add("Equip");
                    handlers.Add(() =>
                    {
                        _inventory.EquipFromSlot(slotIndex);
                        ReturnToMainMenu();
                    });
                }
            }
            else if (item.kind == ItemKind.Consumable)
            {
                actions.Add("Use");
                handlers.Add(() =>
                {
                    if (_inventory.TryUseConsumable(slotIndex, out _))
                    {
                        var used = _onConsumableUsed;
                        Hide();
                        used?.Invoke();
                    }
                    else
                    {
                        ReturnToMainMenu();
                    }
                });
            }

            actions.Add("Discard");
            handlers.Add(() => ConfirmDiscard(slotIndex, item));

            actions.Add("Cancel");
            handlers.Add(ReturnToMainMenu);

            _slotSubMenu.Show(actions,
                onSelect: index =>
                {
                    if (index >= 0 && index < handlers.Count) handlers[index]();
                    else ReturnToMainMenu();
                },
                onCancel: ReturnToMainMenu);
        }

        private void ConfirmDiscard(int slotIndex, InventoryItem item)
        {
            if (_confirmDialog == null)
            {
                _inventory.DiscardSlot(slotIndex);
                ReturnToMainMenu();
                return;
            }

            _confirmDialog.Show(
                $"Discard {item.DisplayName}? This cannot be undone.",
                onYes: () =>
                {
                    _inventory.DiscardSlot(slotIndex);
                    ReturnToMainMenu();
                },
                onNo: ReturnToMainMenu);
        }

        private void ReturnToMainMenu()
        {
            _slotSubMenuOpen = false;
            HasInputFocus = true;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += Navigate;
                InputManager.Instance.OnConfirm += Confirm;
                InputManager.Instance.OnCancel += Cancel;
            }

            UpdateSlotLabels();
            UpdateSelection();
        }

        #region UI construction

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);
            _slotTexts.Clear();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float panelHeight = (UnitInventory.Capacity + 1) * OptionHeight + PanelPadding * 2;

            _root = new GameObject("InventoryMenu");
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(20f, 0f);
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

            // Header
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panel.transform, false);
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(PanelPadding, -PanelPadding);
            headerRect.sizeDelta = new Vector2(-PanelPadding * 2, OptionHeight);

            var headerText = headerGo.AddComponent<TextMeshProUGUI>();
            headerText.text = $"{_unit.name} — Items";
            headerText.fontSize = 18;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.color = new Color(1f, 0.85f, 0.5f, 1f);
            headerText.enableWordWrapping = false;

            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                var rowGo = new GameObject($"Slot{i}");
                rowGo.transform.SetParent(panel.transform, false);
                var rowRect = rowGo.AddComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(PanelPadding + 20f, -PanelPadding - OptionHeight - i * OptionHeight);
                rowRect.sizeDelta = new Vector2(-PanelPadding * 2 - 20f, OptionHeight);

                var tmp = rowGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableWordWrapping = false;

                _slotTexts.Add(tmp);
            }

            UpdateSlotLabels();

            // Cursor indicator
            var cursorGo = new GameObject("Cursor");
            cursorGo.transform.SetParent(panel.transform, false);
            var cursorRect = cursorGo.AddComponent<RectTransform>();
            cursorRect.anchorMin = new Vector2(0f, 1f);
            cursorRect.anchorMax = new Vector2(0f, 1f);
            cursorRect.pivot = new Vector2(0f, 0.5f);
            cursorRect.sizeDelta = new Vector2(20f, OptionHeight);
            cursorRect.anchoredPosition = new Vector2(PanelPadding, -PanelPadding - OptionHeight - OptionHeight * 0.5f);

            _cursorIndicator = cursorGo.AddComponent<TextMeshProUGUI>();
            _cursorIndicator.text = "\u25B6";
            _cursorIndicator.fontSize = 16;
            _cursorIndicator.alignment = TextAlignmentOptions.Center;
            _cursorIndicator.color = TextSelected;
            _cursorIndicator.enableWordWrapping = false;

            _root.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        private void UpdateSlotLabels()
        {
            int equippedSlot = _inventory.EquippedWeaponSlot;
            for (int i = 0; i < _slotTexts.Count; i++)
            {
                var slot = _inventory.GetSlot(i);
                _slotTexts[i].text = FormatSlot(i, slot, i == equippedSlot);
                _slotTexts[i].color = ColorForSlot(slot);
            }
        }

        private static string FormatSlot(int index, InventoryItem item, bool equipped)
        {
            string mark = equipped ? "★" : " ";
            if (item.IsEmpty) return $"{mark} {index + 1}. —";

            string uses = item.Indestructible
                ? "∞"
                : $"{item.CurrentUses}/{item.MaxUses}";
            return $"{mark} {index + 1}. {item.DisplayName}  ({uses})";
        }

        private static Color ColorForSlot(InventoryItem item)
        {
            if (item.IsEmpty) return TextEmpty;
            if (item.IsDepleted) return TextBroken;
            return TextNormal;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _slotTexts.Count; i++)
            {
                var slot = _inventory.GetSlot(i);
                var baseColor = ColorForSlot(slot);
                _slotTexts[i].color = i == _selectedIndex ? TextSelected : baseColor;
            }

            if (_cursorIndicator != null)
            {
                var rect = _cursorIndicator.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    PanelPadding,
                    -PanelPadding - OptionHeight - _selectedIndex * OptionHeight - OptionHeight * 0.5f);
            }
        }

        #endregion
    }
}
