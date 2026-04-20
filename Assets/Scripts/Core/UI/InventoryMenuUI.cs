using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Drives the Indigo Codex inventory popup — a five-slot modal that lists a unit's
    /// inventory, previews the selected item's stats, and routes Confirm into the
    /// Equip / Use / Discard / Cancel sub-menu. Visuals come from the
    /// InventoryPopup.prefab built by InventoryPopupBuilder; this controller only binds
    /// live data and handles input. Navigation semantics match the original primitive UI
    /// so GridCursor, UnitActionMenuUI, and ConfirmDialogUI integrations are unchanged.
    /// </summary>
    public class InventoryMenuUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject _popupInstance;

        // Row-state text colors — empty/depleted cases use distinct palette entries
        // matching the mockup JSX so rows in the same list feel visually coherent.
        private static readonly Color NameNormal   = new(0.95f, 0.90f, 0.77f, 1f);   // #f2e6c4
        private static readonly Color NameSelected = new(1.00f, 0.96f, 0.85f, 1f);   // #fff5d8
        private static readonly Color NameEmpty    = new(0.95f, 0.90f, 0.77f, 0.32f);
        private static readonly Color NameDepleted = new(0.69f, 0.22f, 0.16f, 1f);   // vermillion
        private static readonly Color KindNormal   = new(0.79f, 0.72f, 0.54f, 1f);   // parchDim
        private static readonly Color KindSelected = new(0.99f, 0.89f, 0.60f, 1f);   // brassGlow
        private static readonly Color SigilBrass   = new(0.91f, 0.78f, 0.42f, 1f);   // brassLite
        private static readonly Color SigilGlow    = new(0.99f, 0.89f, 0.60f, 1f);   // brassGlow
        private static readonly Color SigilEmpty   = new(0.79f, 0.60f, 0.23f, 0.35f);
        private static readonly Color BarBrass     = new(0.79f, 0.60f, 0.23f, 1f);   // brass
        private static readonly Color BarVermillion = new(0.69f, 0.22f, 0.16f, 1f);

        private UnitInventory _inventory;
        private TestUnit _unit;
        private ConfirmDialogUI _confirmDialog;
        private UnitActionMenuUI _slotSubMenu;
        private Action _onConsumableUsed;
        private Action _onClose;

        private InventoryPopupRefs _refs;
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
            ActivateUI();
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

            if (_popupInstance != null) _popupInstance.SetActive(false);
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

            // Copy the visual-asset wiring (bg sprite, cursor, divider, font, glow material)
            // from the scene-wired action menu on the GridCursor. Without this the sub-menu's
            // _optionFont is null → UpdateSelection's fontMaterial reset throws NRE inside TMP.
            var template = FindAnyObjectByType<UnitActionMenuUI>(FindObjectsInactive.Include);
            if (template != null && template != _slotSubMenu)
                _slotSubMenu.CopyAssetsFrom(template);
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

            // Release main menu input so the sub-menu owns Up/Down/Confirm/Cancel.
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
                    if (item.consumable.type == ConsumableType.StatBooster && _confirmDialog != null)
                    {
                        ConfirmStatBoosterUse(slotIndex, item);
                        return;
                    }

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

        private void ConfirmStatBoosterUse(int slotIndex, InventoryItem item)
        {
            var (message, _) = ConsumableEffects.DescribeStatBoost(item.consumable, _unit);
            _confirmDialog.Show(message,
                onYes: () =>
                {
                    if (_inventory.TryUseConsumable(slotIndex, out string failReason))
                    {
                        var used = _onConsumableUsed;
                        Hide();
                        used?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning($"[Inventory] Stat booster failed: {failReason}");
                        ReturnToMainMenu();
                    }
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

        #region UI binding

        private void ActivateUI()
        {
            if (_popupInstance == null)
            {
                Debug.LogError("InventoryMenuUI: _popupInstance not wired. Run the scene setup " +
                    "menu or re-run 'Project Astra/Build Inventory Popup (prefab)'.");
                return;
            }

            if (_refs == null) _refs = _popupInstance.GetComponent<InventoryPopupRefs>();
            if (_refs == null)
            {
                Debug.LogError("InventoryMenuUI: popup instance has no InventoryPopupRefs — rebuild the prefab.");
                return;
            }

            _popupInstance.SetActive(true);
            // Ensure the popup sits on top of anything else added to the canvas after setup.
            _popupInstance.transform.SetAsLastSibling();

            BindUnitInfo();
            UpdateSlotLabels();
        }

        private void BindUnitInfo()
        {
            if (_refs.unitName != null) _refs.unitName.text = _unit != null ? _unit.name : "—";
            if (_refs.unitClass != null) _refs.unitClass.text = ClassLabel(_unit);

            int hp = _unit != null ? _unit.currentHP : 0;
            int maxHp = _unit != null ? _unit.maxHP : 1;
            if (_refs.hpNumbers != null) _refs.hpNumbers.text = $"{hp} / {maxHp}";
            if (_refs.hpFill != null)
            {
                float frac = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
                var rt = _refs.hpFill.rectTransform;
                rt.sizeDelta = new Vector2(288f * frac, rt.sizeDelta.y);
            }

            if (_refs.inventoryCount != null)
            {
                int occupied = _inventory != null ? _inventory.OccupiedCount : 0;
                _refs.inventoryCount.text = $"{occupied} / {UnitInventory.Capacity}";
            }
        }

        private static string ClassLabel(TestUnit unit)
        {
            if (unit == null) return "—";
            var cls = unit.UnitInstance?.CurrentClass;
            if (cls != null && !string.IsNullOrEmpty(cls.ClassName)) return cls.ClassName.ToUpperInvariant();
            return unit.faction == Faction.Player ? "ALLIED UNIT" : unit.faction.ToString().ToUpperInvariant();
        }

        private void UpdateSlotLabels()
        {
            if (_refs == null) return;
            int equippedSlot = _inventory != null ? _inventory.EquippedWeaponSlot : -1;

            for (int i = 0; i < _refs.rows.Length; i++)
            {
                var rowRefs = _refs.rows[i];
                var slot = _inventory != null ? _inventory.GetSlot(i) : InventoryItem.None;
                ApplySlotToRow(rowRefs, i, slot, i == equippedSlot);
            }
        }

        private void ApplySlotToRow(InventoryPopupRefs.RowRefs row, int slotIndex,
            InventoryItem item, bool equipped)
        {
            if (row == null || row.root == null) return;

            bool empty    = item.IsEmpty;
            bool depleted = !empty && item.IsDepleted;

            // Sigil + background sprite per state (selected overlay applied later by UpdateSelection).
            if (empty)
            {
                row.background.sprite = row.sprEmpty;
                row.sigil.sprite = null;
                row.sigil.color = SigilEmpty;
                row.nameText.text = "— vacant —";
                row.nameText.color = NameEmpty;
                row.nameText.fontStyle = FontStyles.Italic;
                row.kindText.text = "";
                row.usesText.text = "·  /  ·";
                row.usesText.color = NameEmpty;
                row.durabilityTrack.gameObject.SetActive(false);
                row.durabilityFill.gameObject.SetActive(false);
            }
            else
            {
                row.background.sprite = depleted ? row.sprDepleted : row.sprDefault;
                row.sigil.sprite = SigilFor(item);
                row.sigil.color = SigilBrass;
                row.nameText.text = equipped ? $"★ {item.DisplayName}" : item.DisplayName;
                row.nameText.color = depleted ? NameDepleted : NameNormal;
                row.nameText.fontStyle = FontStyles.Bold;
                row.kindText.text = KindLabel(item);
                row.kindText.color = KindNormal;
                row.usesText.text = item.Indestructible ? "∞" : $"{item.CurrentUses} / {item.MaxUses}";
                row.usesText.color = depleted ? NameDepleted : NameNormal;

                row.durabilityTrack.gameObject.SetActive(true);
                row.durabilityFill.gameObject.SetActive(true);
                float frac = item.Indestructible || item.MaxUses == 0
                    ? 1f
                    : Mathf.Clamp01((float)item.CurrentUses / item.MaxUses);
                var fillRt = row.durabilityFill.rectTransform;
                fillRt.sizeDelta = new Vector2(80f * frac, fillRt.sizeDelta.y);
                row.durabilityFill.color = depleted ? BarVermillion : BarBrass;
            }

            row.selectionCaret.gameObject.SetActive(false);
        }

        private static string KindLabel(InventoryItem item)
        {
            switch (item.kind)
            {
                case ItemKind.Weapon:
                    return item.weapon.weaponType + (item.weapon.brave ? " · Brave" : "");
                case ItemKind.Consumable:
                    return item.consumable.type == ConsumableType.Vulnerary
                        ? "Elixir · restorative"
                        : "Stat Boost · " + item.consumable.targetStat;
                default:
                    return "";
            }
        }

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
                _                => _refs.sigilSword, // tomes fall back to sword sigil for now
            };
        }

        private void UpdateSelection()
        {
            if (_refs == null) return;

            for (int i = 0; i < _refs.rows.Length; i++)
            {
                var row = _refs.rows[i];
                if (row == null || row.root == null) continue;

                bool selected = i == _selectedIndex;
                var slot = _inventory != null ? _inventory.GetSlot(i) : InventoryItem.None;
                bool empty = slot.IsEmpty;
                bool depleted = !empty && slot.IsDepleted;

                row.selectionCaret.gameObject.SetActive(selected && !empty);

                if (selected && !empty)
                {
                    row.background.sprite = row.sprSelected;
                    row.nameText.color = NameSelected;
                    row.kindText.color = KindSelected;
                    row.sigil.color = SigilGlow;
                    row.usesText.color = NameSelected;
                }
                else if (empty)
                {
                    row.background.sprite = row.sprEmpty;
                }
                else
                {
                    row.background.sprite = depleted ? row.sprDepleted : row.sprDefault;
                    row.nameText.color = depleted ? NameDepleted : NameNormal;
                    row.kindText.color = KindNormal;
                    row.sigil.color = SigilBrass;
                    row.usesText.color = depleted ? NameDepleted : NameNormal;
                }
            }

            BindStatsPanel(_inventory != null ? _inventory.GetSlot(_selectedIndex) : InventoryItem.None);
        }

        private void BindStatsPanel(InventoryItem item)
        {
            if (_refs == null) return;

            if (item.IsEmpty)
            {
                if (_refs.statsGroup != null) _refs.statsGroup.alpha = 0.45f;
                _refs.selectedItemName.text = "— select an item —";
                _refs.selectedItemKind.text = "";
                SetStat(_refs.statAtk, null);
                SetStat(_refs.statHit, null);
                SetStat(_refs.statRng, null);
                SetStat(_refs.statWt,  null);
                _refs.itemDescription.text = "";
                _refs.provText.text = "";
                return;
            }

            if (_refs.statsGroup != null) _refs.statsGroup.alpha = 1f;
            _refs.selectedItemName.text = item.DisplayName;

            if (item.kind == ItemKind.Weapon)
            {
                var w = item.weapon;
                _refs.selectedItemKind.text = $"{w.weaponType.ToString().ToUpperInvariant()} · {w.tier.ToString().ToUpperInvariant()}";
                SetStat(_refs.statAtk, w.might);
                SetStat(_refs.statHit, w.hit);
                SetStat(_refs.statRng, FormatRange(w.minRange, w.maxRange));
                SetStat(_refs.statWt,  w.weight);
                _refs.itemDescription.text = DescribeWeapon(w);
                _refs.provText.text = w.characterLocked && !string.IsNullOrEmpty(w.ownerUnitId)
                    ? $"Bound to {w.ownerUnitId}"
                    : $"{w.minRank} rank required";
            }
            else // Consumable
            {
                var c = item.consumable;
                _refs.selectedItemKind.text = c.type == ConsumableType.Vulnerary
                    ? "ELIXIR · RESTORATIVE"
                    : "STAT BOOST";
                SetStat(_refs.statAtk, null);
                SetStat(_refs.statHit, null);
                SetStat(_refs.statRng, null);
                SetStat(_refs.statWt,  null);
                _refs.itemDescription.text = DescribeConsumable(c);
                _refs.provText.text = $"{c.currentUses} / {c.maxUses} uses remaining";
            }
        }

        private static void SetStat(TextMeshProUGUI field, object value)
        {
            if (field == null) return;
            field.text = value == null ? "—" : value.ToString();
        }

        private static string FormatRange(int min, int max)
            => min == max ? min.ToString() : $"{min}–{max}";

        private static string DescribeWeapon(WeaponData w)
        {
            if (w.staffEffect != StaffEffect.None)
                return w.staffEffect == StaffEffect.AreaOfEffect
                    ? "Restores HP to every ally within range. Consumes a use per cast."
                    : "Channels divine energy to mend a single ally's wounds.";
            if (w.IsEffectiveAgainst(ClassType.Cavalry) || w.IsEffectiveAgainst(ClassType.Armoured))
                return "Shaped to cleave specific enemy types — strike hard when the target fits.";
            return w.brave
                ? "Strikes twice per attack. Weight tires the wielder across rounds."
                : $"Standard {w.weaponType.ToString().ToLowerInvariant()}. Reliable in open combat.";
        }

        private static string DescribeConsumable(ConsumableData c)
        {
            return c.type == ConsumableType.Vulnerary
                ? $"Restores {c.magnitude} HP when used. Field-tested across campaigns."
                : $"Permanently boosts {c.targetStat} by {c.magnitude} when consumed.";
        }

        #endregion
    }
}
