using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Convoy
{
    // Supply Convoy trade screen (Indigo Codex · Variant A). Drives the
    // SupplyConvoy.prefab via SupplyConvoyRefs. Navigation:
    //   • opens on Submenu focus; Left/Right toggles Give ↔ Take
    //   • Down enters UnitInv (Give) or ConvoyList (Take)
    //   • in list: Up/Down navigates rows, Left/Right cycles category tabs,
    //     Confirm executes the current mode (store / withdraw); Up past the
    //     top returns to the Submenu
    //   • Cancel at any point closes and consumes the lord's action.
    // Game logic (TryWithdraw, TryStore) is identical to the legacy primitive UI.
    public class ConvoyUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject popupInstance;

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

        private SupplyConvoy convoy;
        private TestUnit unit;
        private ToastNotificationUI toastUI;
        private Action onClose;

        private SupplyConvoyRefs refs;
        private Mode mode;
        private Zone zone;
        private int unitCursor;          // 0..4
        private int convoyCursor;        // 0..visible rows-1
        private int convoyScrollOffset;  // offset into filtered list
        private int tabIndex;            // 0..9 (All / Sword / Lance / Axe / Bow / Anima / Light / Dark / Staff / Consumable)
        private readonly List<int> filtered = new(); // indices into _convoy.ToArray() matching current tab

        public void Show(SupplyConvoy convoy, TestUnit lordUnit,
            ToastNotificationUI toastUI, Action onClose)
        {
            this.convoy = convoy;
            unit = lordUnit;
            this.toastUI = toastUI;
            this.onClose = onClose;
            mode = Mode.Take;
            zone = Zone.Submenu;
            unitCursor = 0;
            convoyCursor = 0;
            convoyScrollOffset = 0;
            tabIndex = 0;

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

            bool wasOpen = popupInstance != null && popupInstance.activeSelf;
            if (popupInstance != null) popupInstance.SetActive(false);
            if (wasOpen) AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        #region UI activation

        private void ActivateUI()
        {
            if (popupInstance == null)
            {
                Debug.LogError("ConvoyUI: _popupInstance not wired. Run the scene setup or " +
                    "'Project Astra/Build Supply Convoy (prefab)' + re-run CursorSceneSetup.");
                return;
            }
            if (refs == null) refs = popupInstance.GetComponent<SupplyConvoyRefs>();
            if (refs == null)
            {
                Debug.LogError("ConvoyUI: popup has no SupplyConvoyRefs — rebuild the prefab.");
                return;
            }
            popupInstance.SetActive(true);
            popupInstance.transform.SetAsLastSibling();
        }

        #endregion

        #region Input

        private void Navigate(Vector2Int dir)
        {
            if (refs == null) return;

            if (zone == Zone.Submenu)
            {
                if (dir.x < 0) { mode = Mode.Give; RefreshSubmenu(); RefreshBubble(); RefreshConvoy(); AudioManager.Instance?.Play(SoundId.UiTab); }
                else if (dir.x > 0) { mode = Mode.Take; RefreshSubmenu(); RefreshBubble(); RefreshConvoy(); AudioManager.Instance?.Play(SoundId.UiTab); }
                else if (dir.y < 0)
                {
                    zone = mode == Mode.Give ? Zone.UnitInv : Zone.ConvoyList;
                    RefreshFocus();
                    AudioManager.Instance?.Play(SoundId.UiMove);
                }
                return;
            }

            if (zone == Zone.UnitInv)
            {
                if (dir.y > 0)
                {
                    if (unitCursor == 0) { zone = Zone.Submenu; RefreshFocus(); return; }
                    unitCursor--;
                }
                else if (dir.y < 0)
                {
                    unitCursor = Mathf.Min(UnitInventory.Capacity - 1, unitCursor + 1);
                }
                RefreshUnitInv();
                AudioManager.Instance?.Play(SoundId.UiMove);
                return;
            }

            // Zone.ConvoyList
            if (dir.x != 0)
            {
                int step = dir.x > 0 ? 1 : -1;
                tabIndex = (tabIndex + step + 10) % 10;
                convoyCursor = 0;
                convoyScrollOffset = 0;
                RebuildFiltered();
                RefreshTabs();
                RefreshConvoy();
                AudioManager.Instance?.Play(SoundId.UiTab);
                return;
            }
            if (dir.y == 0) return;

            int visible = refs.rows.Length;
            int count = filtered.Count;
            if (count == 0)
            {
                if (dir.y > 0) { zone = Zone.Submenu; RefreshFocus(); }
                return;
            }

            int absIndex = convoyScrollOffset + convoyCursor + (dir.y > 0 ? -1 : 1);

            if (absIndex < 0)
            {
                zone = Zone.Submenu;
                RefreshFocus();
                return;
            }
            if (absIndex >= count) absIndex = count - 1;

            if (absIndex < convoyScrollOffset)        convoyScrollOffset = absIndex;
            else if (absIndex >= convoyScrollOffset + visible) convoyScrollOffset = absIndex - visible + 1;

            convoyCursor = absIndex - convoyScrollOffset;
            RefreshConvoy();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void Confirm()
        {
            if (refs == null) return;

            if (zone == Zone.Submenu)
            {
                zone = mode == Mode.Give ? Zone.UnitInv : Zone.ConvoyList;
                RefreshFocus();
                return;
            }

            if (zone == Zone.UnitInv) TryStore();
            else if (zone == Zone.ConvoyList) TryWithdraw();
        }

        private void Cancel()
        {
            var close = onClose;
            Hide();
            close?.Invoke();
        }

        #endregion

        #region Give / Take actions

        private void TryStore()
        {
            var inv = unit.Inventory;
            var item = inv.GetSlot(unitCursor);
            if (item.IsEmpty) return;
            if (convoy.IsFull) { AudioManager.Instance?.Play(SoundId.UiInvalid); toastUI?.Show("Convoy full"); return; }

            inv.DiscardSlot(unitCursor);
            convoy.TryDeposit(item);
            AudioManager.Instance?.Play(SoundId.ItemMove);

            RebuildFiltered();
            RefreshAll();
        }

        private void TryWithdraw()
        {
            int count = filtered.Count;
            if (count == 0) return;
            int absIndex = filtered[Mathf.Clamp(convoyScrollOffset + convoyCursor, 0, count - 1)];

            var item = convoy.GetSlot(absIndex);
            if (item.IsEmpty) return;

            var inv = unit.Inventory;
            if (inv.IsFull) { AudioManager.Instance?.Play(SoundId.UiInvalid); toastUI?.Show("Inventory full"); return; }

            if (!convoy.TryWithdraw(absIndex, out var withdrawn)) return;
            inv.TryAddItem(withdrawn, out _);
            AudioManager.Instance?.Play(SoundId.ItemMove);

            RebuildFiltered();
            int newCount = filtered.Count;
            int maxScroll = Mathf.Max(0, newCount - refs.rows.Length);
            if (convoyScrollOffset > maxScroll) convoyScrollOffset = maxScroll;
            convoyCursor = Mathf.Clamp(convoyCursor, 0, Mathf.Max(0, newCount - 1 - convoyScrollOffset));
            RefreshAll();
        }

        #endregion

        #region Filtering by category

        private void RebuildFiltered()
        {
            filtered.Clear();
            if (convoy == null) return;
            var all = convoy.ToArray();
            for (int i = 0; i < all.Length; i++)
            {
                if (MatchesTab(all[i], tabIndex)) filtered.Add(i);
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
            if (refs.portraitName != null) refs.portraitName.text = unit != null ? unit.name : "—";
            if (refs.stockNum != null)
                refs.stockNum.text = $"{convoy.Count} / {SupplyConvoy.MaxCapacity}";
            if (refs.lordInvCap != null)
                refs.lordInvCap.text = $"{unit.Inventory.OccupiedCount} / {UnitInventory.Capacity}";
        }

        private void RefreshBubble()
        {
            if (refs.bubbleLine == null) return;
            refs.bubbleLine.text = mode == Mode.Give
                ? "\u201CI can spare this one. Keep it with the convoy.\u201D"
                : "\u201CThe convoy has what I need. Mark it to my side.\u201D";
        }

        private void RefreshSubmenu()
        {
            if (refs == null) return;
            bool give = mode == Mode.Give;
            ApplySubmenuState(refs.giveButtonBg, refs.giveLabel, give, zone == Zone.Submenu && give);
            ApplySubmenuState(refs.takeButtonBg, refs.takeLabel, !give, zone == Zone.Submenu && !give);
        }

        private void ApplySubmenuState(Image bg, TextMeshProUGUI label, bool active, bool focused)
        {
            if (bg == null) return;
            Sprite target = refs.submenuDefault;
            if (active) target = refs.submenuActive;
            else if (focused) target = refs.submenuHover;
            bg.sprite = target;
            if (label != null) label.color = active ? ColBrassGlow : (focused ? ColParchmentSel : ColParchment);
        }

        private void RefreshUnitInv()
        {
            var inv = unit.Inventory;
            int equipped = inv.EquippedWeaponSlot;
            for (int i = 0; i < refs.slots.Length; i++)
            {
                var slot = refs.slots[i];
                if (slot == null || slot.root == null) continue;
                var item = inv.GetSlot(i);
                ApplySlot(slot, item, i == equipped, zone == Zone.UnitInv && i == unitCursor);
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
            if (refs.tabs == null) return;
            int[] counts = new int[10];
            var arr = convoy.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                counts[0]++;
                for (int t = 1; t < 10; t++) if (MatchesTab(arr[i], t)) counts[t]++;
            }

            for (int i = 0; i < refs.tabs.Length; i++)
            {
                var tab = refs.tabs[i];
                if (tab == null || tab.root == null) continue;
                bool active = i == tabIndex;
                bool focused = zone == Zone.ConvoyList && i == tabIndex;
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
            var arr = convoy.ToArray();
            int visible = refs.rows.Length;

            for (int i = 0; i < visible; i++)
            {
                var row = refs.rows[i];
                if (row == null || row.root == null) continue;

                int filteredIdx = convoyScrollOffset + i;
                if (filteredIdx >= filtered.Count)
                {
                    row.root.SetActive(false);
                    continue;
                }
                row.root.SetActive(true);
                int absIdx = filtered[filteredIdx];
                var item = arr[absIdx];
                bool focused = zone == Zone.ConvoyList && i == convoyCursor;
                bool depleted = item.IsDepleted;
                bool disabled = mode == Mode.Take && unit.Inventory.IsFull;

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
            if (refs.scrollThumb != null && filtered.Count > 0)
            {
                float ratio = (float)convoyScrollOffset / Mathf.Max(1, filtered.Count);
                var thumbRt = refs.scrollThumb;
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
            if (item.kind == ItemKind.Consumable) return refs.sigilConsumable;
            if (item.kind != ItemKind.Weapon) return null;
            return item.weapon.weaponType switch
            {
                WeaponType.Sword => refs.sigilSword,
                WeaponType.Lance => refs.sigilLance,
                WeaponType.Axe   => refs.sigilAxe,
                WeaponType.Bow   => refs.sigilBow,
                WeaponType.Staff => refs.sigilStaff,
                _                => refs.sigilSword,
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
