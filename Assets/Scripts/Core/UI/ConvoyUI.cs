using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Supply Convoy trade screen (Indigo Codex · Variant A). Drives the
    /// SupplyConvoy.prefab via SupplyConvoyRefs. Navigation model:
    ///   • opens on Submenu focus; Left/Right toggles Give ↔ Take
    ///   • Down enters UnitInv (Give) or ConvoyList (Take)
    ///   • inside list: Up/Down navigates rows, Left/Right cycles category tabs,
    ///     Confirm executes the current mode (store / withdraw), Up past the top
    ///     returns to the Submenu
    ///   • Cancel at any point closes and consumes the lord's action.
    /// Game logic (TryWithdraw, TryStore) is identical to the legacy primitive UI.
    /// </summary>
    public class ConvoyUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject _popupInstance;

        private enum Mode { Give, Take }
        private enum Zone { Submenu, UnitInv, ConvoyList }

        // Colors match the mockup palette
        private static readonly Color ColParchment    = new(0.95f, 0.90f, 0.77f, 1f);    // #f2e6c4
        private static readonly Color ColParchmentSel = new(1.00f, 0.96f, 0.85f, 1f);    // #fff5d8
        private static readonly Color ColParchDim     = new(0.79f, 0.72f, 0.54f, 1f);    // #c9b98a
        private static readonly Color ColBrassLite    = new(0.91f, 0.78f, 0.42f, 1f);    // #e8c66a
        private static readonly Color ColBrassGlow    = new(0.99f, 0.89f, 0.60f, 1f);    // #fde49a
        private static readonly Color ColVermillion   = new(0.69f, 0.22f, 0.16f, 1f);    // #b0382a
        private static readonly Color ColBrass        = new(0.79f, 0.60f, 0.23f, 1f);    // #c9993a

        private SupplyConvoy _convoy;
        private TestUnit _unit;
        private ToastNotificationUI _toastUI;
        private Action _onClose;

        private SupplyConvoyRefs _refs;
        private Mode _mode;
        private Zone _zone;
        private int _unitCursor;          // 0..4
        private int _convoyCursor;        // 0..visible rows-1
        private int _convoyScrollOffset;  // offset into filtered list
        private int _tabIndex;            // 0..9 (All / Sword / Lance / Axe / Bow / Anima / Light / Dark / Staff / Consumable)
        private readonly List<int> _filtered = new(); // indices into _convoy.ToArray() matching current tab

        public void Show(SupplyConvoy convoy, TestUnit lordUnit,
            ToastNotificationUI toastUI, Action onClose)
        {
            _convoy = convoy;
            _unit = lordUnit;
            _toastUI = toastUI;
            _onClose = onClose;
            _mode = Mode.Take;
            _zone = Zone.Submenu;
            _unitCursor = 0;
            _convoyCursor = 0;
            _convoyScrollOffset = 0;
            _tabIndex = 0;

            ActivateUI();
            RebuildFiltered();
            RefreshAll();

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

            if (_popupInstance != null) _popupInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        #region UI activation

        private void ActivateUI()
        {
            if (_popupInstance == null)
            {
                Debug.LogError("ConvoyUI: _popupInstance not wired. Run the scene setup or " +
                    "'Project Astra/Build Supply Convoy (prefab)' + re-run CursorSceneSetup.");
                return;
            }
            if (_refs == null) _refs = _popupInstance.GetComponent<SupplyConvoyRefs>();
            if (_refs == null)
            {
                Debug.LogError("ConvoyUI: popup has no SupplyConvoyRefs — rebuild the prefab.");
                return;
            }
            _popupInstance.SetActive(true);
            _popupInstance.transform.SetAsLastSibling();
        }

        #endregion

        #region Input

        private void Navigate(Vector2Int dir)
        {
            if (_refs == null) return;

            if (_zone == Zone.Submenu)
            {
                if (dir.x < 0) { _mode = Mode.Give; RefreshSubmenu(); RefreshBubble(); RefreshConvoy(); }
                else if (dir.x > 0) { _mode = Mode.Take; RefreshSubmenu(); RefreshBubble(); RefreshConvoy(); }
                else if (dir.y < 0)
                {
                    _zone = _mode == Mode.Give ? Zone.UnitInv : Zone.ConvoyList;
                    RefreshFocus();
                }
                return;
            }

            if (_zone == Zone.UnitInv)
            {
                if (dir.y > 0)
                {
                    if (_unitCursor == 0) { _zone = Zone.Submenu; RefreshFocus(); return; }
                    _unitCursor--;
                }
                else if (dir.y < 0)
                {
                    _unitCursor = Mathf.Min(UnitInventory.Capacity - 1, _unitCursor + 1);
                }
                RefreshUnitInv();
                return;
            }

            // Zone.ConvoyList
            if (dir.x != 0)
            {
                int step = dir.x > 0 ? 1 : -1;
                _tabIndex = (_tabIndex + step + 10) % 10;
                _convoyCursor = 0;
                _convoyScrollOffset = 0;
                RebuildFiltered();
                RefreshTabs();
                RefreshConvoy();
                return;
            }
            if (dir.y == 0) return;

            int visible = _refs.rows.Length;
            int count = _filtered.Count;
            if (count == 0)
            {
                if (dir.y > 0) { _zone = Zone.Submenu; RefreshFocus(); }
                return;
            }

            int absIndex = _convoyScrollOffset + _convoyCursor + (dir.y > 0 ? -1 : 1);

            if (absIndex < 0)
            {
                _zone = Zone.Submenu;
                RefreshFocus();
                return;
            }
            if (absIndex >= count) absIndex = count - 1;

            if (absIndex < _convoyScrollOffset)        _convoyScrollOffset = absIndex;
            else if (absIndex >= _convoyScrollOffset + visible) _convoyScrollOffset = absIndex - visible + 1;

            _convoyCursor = absIndex - _convoyScrollOffset;
            RefreshConvoy();
        }

        private void Confirm()
        {
            if (_refs == null) return;

            if (_zone == Zone.Submenu)
            {
                _zone = _mode == Mode.Give ? Zone.UnitInv : Zone.ConvoyList;
                RefreshFocus();
                return;
            }

            if (_zone == Zone.UnitInv) TryStore();
            else if (_zone == Zone.ConvoyList) TryWithdraw();
        }

        private void Cancel()
        {
            var close = _onClose;
            Hide();
            close?.Invoke();
        }

        #endregion

        #region Give / Take actions

        private void TryStore()
        {
            var inv = _unit.Inventory;
            var item = inv.GetSlot(_unitCursor);
            if (item.IsEmpty) return;
            if (_convoy.IsFull) { _toastUI?.Show("Convoy full"); return; }

            inv.DiscardSlot(_unitCursor);
            _convoy.TryDeposit(item);

            RebuildFiltered();
            RefreshAll();
        }

        private void TryWithdraw()
        {
            int count = _filtered.Count;
            if (count == 0) return;
            int absIndex = _filtered[Mathf.Clamp(_convoyScrollOffset + _convoyCursor, 0, count - 1)];

            var item = _convoy.GetSlot(absIndex);
            if (item.IsEmpty) return;

            var inv = _unit.Inventory;
            if (inv.IsFull) { _toastUI?.Show("Inventory full"); return; }

            if (!_convoy.TryWithdraw(absIndex, out var withdrawn)) return;
            inv.TryAddItem(withdrawn, out _);

            RebuildFiltered();
            int newCount = _filtered.Count;
            int maxScroll = Mathf.Max(0, newCount - _refs.rows.Length);
            if (_convoyScrollOffset > maxScroll) _convoyScrollOffset = maxScroll;
            _convoyCursor = Mathf.Clamp(_convoyCursor, 0, Mathf.Max(0, newCount - 1 - _convoyScrollOffset));
            RefreshAll();
        }

        #endregion

        #region Filtering by category

        private void RebuildFiltered()
        {
            _filtered.Clear();
            if (_convoy == null) return;
            var all = _convoy.ToArray();
            for (int i = 0; i < all.Length; i++)
            {
                if (MatchesTab(all[i], _tabIndex)) _filtered.Add(i);
            }
        }

        private static bool MatchesTab(InventoryItem item, int tab)
        {
            if (item.IsEmpty) return false;
            if (tab == 0) return true;                   // All
            if (tab == 9) return item.kind == ItemKind.Consumable;
            if (item.kind != ItemKind.Weapon) return false;
            switch (tab)
            {
                case 1: return item.weapon.weaponType == WeaponType.Sword;
                case 2: return item.weapon.weaponType == WeaponType.Lance;
                case 3: return item.weapon.weaponType == WeaponType.Axe;
                case 4: return item.weapon.weaponType == WeaponType.Bow;
                case 5: return item.weapon.weaponType == WeaponType.AnimaTome;
                case 6: return item.weapon.weaponType == WeaponType.LightTome;
                case 7: return item.weapon.weaponType == WeaponType.DarkTome;
                case 8: return item.weapon.weaponType == WeaponType.Staff;
                default: return false;
            }
        }

        #endregion

        #region Refresh passes

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshBubble();
            RefreshSubmenu();
            RefreshUnitInv();
            RefreshTabs();
            RefreshConvoy();
            RefreshFocus();
        }

        private void RefreshHeader()
        {
            if (_refs.portraitName != null) _refs.portraitName.text = _unit != null ? _unit.name : "—";
            if (_refs.stockNum != null)
                _refs.stockNum.text = $"{_convoy.Count} / {SupplyConvoy.MaxCapacity}";
            if (_refs.lordInvCap != null)
                _refs.lordInvCap.text = $"{_unit.Inventory.OccupiedCount} / {UnitInventory.Capacity}";
        }

        private void RefreshBubble()
        {
            if (_refs.bubbleLine == null) return;
            _refs.bubbleLine.text = _mode == Mode.Give
                ? "\u201CI can spare this one. Keep it with the convoy.\u201D"
                : "\u201CThe convoy has what I need. Mark it to my side.\u201D";
        }

        private void RefreshSubmenu()
        {
            if (_refs == null) return;
            bool give = _mode == Mode.Give;
            ApplySubmenuState(_refs.giveButtonBg, _refs.giveLabel, give, _zone == Zone.Submenu && give);
            ApplySubmenuState(_refs.takeButtonBg, _refs.takeLabel, !give, _zone == Zone.Submenu && !give);
        }

        private void ApplySubmenuState(Image bg, TextMeshProUGUI label, bool active, bool focused)
        {
            if (bg == null) return;
            Sprite target = _refs.submenuDefault;
            if (active) target = _refs.submenuActive;
            else if (focused) target = _refs.submenuHover;
            bg.sprite = target;
            if (label != null) label.color = active ? ColBrassGlow : (focused ? ColParchmentSel : ColParchment);
        }

        private void RefreshUnitInv()
        {
            var inv = _unit.Inventory;
            int equipped = inv.EquippedWeaponSlot;
            for (int i = 0; i < _refs.slots.Length; i++)
            {
                var slot = _refs.slots[i];
                if (slot == null || slot.root == null) continue;
                var item = inv.GetSlot(i);
                ApplySlot(slot, item, i == equipped, _zone == Zone.UnitInv && i == _unitCursor);
            }
        }

        private void ApplySlot(SupplyConvoyRefs.SlotRefs slot, InventoryItem item, bool equipped, bool focused)
        {
            bool empty = item.IsEmpty;
            bool depleted = !empty && item.IsDepleted;

            slot.background.sprite = focused ? slot.sprFocused
                                    : empty    ? slot.sprEmpty
                                    : equipped ? slot.sprEquipped
                                    : depleted ? slot.sprDepleted
                                    : slot.sprDefault;

            if (empty)
            {
                slot.sigil.enabled = false;
                slot.nameText.text = "— empty —";
                slot.nameText.color = ColParchDim;
                slot.nameText.fontStyle = FontStyles.Italic;
                slot.rankText.text = "";
                slot.usesText.text = "";
            }
            else
            {
                slot.sigil.enabled = true;
                slot.sigil.sprite = SigilFor(item);
                slot.sigil.color = focused ? ColBrassGlow : ColBrassLite;

                slot.nameText.text = (equipped ? "★ " : "") + item.DisplayName;
                slot.nameText.color = focused ? ColParchmentSel : (depleted ? ColVermillion : (equipped ? ColBrassGlow : ColParchment));
                slot.nameText.fontStyle = FontStyles.Bold;

                slot.rankText.text = item.kind == ItemKind.Weapon ? $"{item.weapon.tier}" : "";
                slot.rankText.color = ColParchDim;

                slot.usesText.text = item.Indestructible ? "∞" : $"{item.CurrentUses} / {item.MaxUses}";
                slot.usesText.color = depleted ? ColVermillion : ColParchmentSel;
            }
        }

        private void RefreshTabs()
        {
            if (_refs.tabs == null) return;
            int[] counts = new int[10];
            var arr = _convoy.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                counts[0]++;
                for (int t = 1; t < 10; t++) if (MatchesTab(arr[i], t)) counts[t]++;
            }

            for (int i = 0; i < _refs.tabs.Length; i++)
            {
                var tab = _refs.tabs[i];
                if (tab == null || tab.root == null) continue;
                bool active = i == _tabIndex;
                bool focused = _zone == Zone.ConvoyList && i == _tabIndex;
                tab.background.sprite = active ? tab.sprActive
                                      : focused ? tab.sprFocused
                                      : tab.sprDefault;
                if (tab.label != null)  tab.label.color    = active ? ColBrassGlow : ColParchDim;
                if (tab.countText != null) {
                    tab.countText.text = counts[i].ToString();
                    tab.countText.color = active ? ColBrassGlow : ColParchDim;
                }
            }
        }

        private void RefreshConvoy()
        {
            var arr = _convoy.ToArray();
            int visible = _refs.rows.Length;

            for (int i = 0; i < visible; i++)
            {
                var row = _refs.rows[i];
                if (row == null || row.root == null) continue;

                int filteredIdx = _convoyScrollOffset + i;
                if (filteredIdx >= _filtered.Count)
                {
                    row.root.SetActive(false);
                    continue;
                }
                row.root.SetActive(true);
                int absIdx = _filtered[filteredIdx];
                var item = arr[absIdx];
                bool focused = _zone == Zone.ConvoyList && i == _convoyCursor;
                bool depleted = item.IsDepleted;
                bool disabled = _mode == Mode.Take && _unit.Inventory.IsFull;

                row.background.sprite = focused ? row.sprFocused
                                      : disabled ? row.sprDisabled
                                      : depleted ? row.sprDepleted
                                      : row.sprDefault;

                row.sigil.sprite = SigilFor(item);
                row.sigil.color = focused ? ColBrassGlow : ColBrassLite;

                row.nameText.text = item.DisplayName;
                row.nameText.color = focused ? ColParchmentSel : (depleted ? ColVermillion : ColParchment);
                row.subText.text = DescribeItem(item);
                row.subText.color = ColParchDim;

                row.rankText.text = item.kind == ItemKind.Weapon ? item.weapon.tier.ToString() : "";

                row.usesText.text = item.Indestructible ? "∞" : $"{item.CurrentUses} / {item.MaxUses}";
                row.usesText.color = depleted ? ColVermillion : ColParchmentSel;

                float frac = item.Indestructible || item.MaxUses == 0
                    ? 1f
                    : Mathf.Clamp01((float)item.CurrentUses / item.MaxUses);
                var fillRt = row.durabilityFill.rectTransform;
                fillRt.sizeDelta = new Vector2(120f * frac, fillRt.sizeDelta.y);
                row.durabilityFill.color = depleted ? ColVermillion : ColBrass;
            }

            // Scroll thumb
            if (_refs.scrollThumb != null && _filtered.Count > 0)
            {
                float ratio = (float)_convoyScrollOffset / Mathf.Max(1, _filtered.Count);
                var thumbRt = _refs.scrollThumb;
                var trackH = ((RectTransform)thumbRt.parent).rect.height;
                thumbRt.anchoredPosition = new Vector2(thumbRt.anchoredPosition.x, -ratio * trackH);
            }
        }

        private void RefreshFocus()
        {
            RefreshSubmenu();
            RefreshUnitInv();
            RefreshTabs();
            RefreshConvoy();
        }

        #endregion

        #region Helpers

        private Sprite SigilFor(InventoryItem item)
        {
            if (item.kind == ItemKind.Consumable) return _refs.sigilConsumable;
            if (item.kind != ItemKind.Weapon) return null;
            return item.weapon.weaponType switch
            {
                WeaponType.Sword => _refs.sigilSword,
                WeaponType.Lance => _refs.sigilLance,
                WeaponType.Axe   => _refs.sigilAxe,
                WeaponType.Bow   => _refs.sigilBow,
                WeaponType.Staff => _refs.sigilStaff,
                _                => _refs.sigilSword,
            };
        }

        private static string DescribeItem(InventoryItem item)
        {
            if (item.kind == ItemKind.Weapon)
            {
                var w = item.weapon;
                var parts = new List<string>();
                parts.Add($"{w.tier} {w.weaponType}");
                if (w.brave) parts.Add("brave · 2-hit");
                if (w.staffEffect != StaffEffect.None) parts.Add("staff · heal");
                return string.Join(" · ", parts);
            }
            if (item.kind == ItemKind.Consumable)
            {
                var c = item.consumable;
                return c.type == ConsumableType.Vulnerary
                    ? $"elixir · restore {c.magnitude} HP"
                    : $"stat boost · {c.targetStat} +{c.magnitude}";
            }
            return "";
        }

        #endregion
    }
}
