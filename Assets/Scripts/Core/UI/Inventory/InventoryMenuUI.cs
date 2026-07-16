using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Inventory
{
    // Drives the Indigo Codex inventory popup — a five-slot modal that lists
    // a unit's inventory, previews the selected item's stats, and routes
    // Confirm into the Equip / Use / Discard / Cancel sub-menu. Visuals come
    // from the InventoryPopup.prefab built by InventoryPopupBuilder; this
    // controller only binds live data and handles input. Navigation
    // semantics match the original primitive UI, so GridCursor,
    // UnitActionMenuUI, and ConfirmDialogUI integrations are unchanged.
    public class InventoryMenuUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject popupInstance;

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

        private UnitInventory inventory;
        private TestUnit unit;
        private ConfirmDialogUI confirmDialog;
        private UnitActionMenuUI slotSubMenu;
        private Action onConsumableUsed;
        private Action onClose;

        private InventoryPopupRefs refs;
        private int selectedIndex;
        private bool slotSubMenuOpen;

        public void Show(TestUnit unit, ConfirmDialogUI confirmDialog,
            Action onConsumableUsed, Action onClose)
        {
            gameObject.SetActive(true); // root must be active so the slot sub-menu child can render
            this.unit = unit;
            inventory = unit?.Inventory;
            this.confirmDialog = confirmDialog;
            this.onConsumableUsed = onConsumableUsed;
            this.onClose = onClose;
            selectedIndex = 0;
            slotSubMenuOpen = false;

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

            gameObject.SetActive(false); // back to disabled-by-default once the menu closes
        }

        private void OnDestroy()
        {
            if (HasInputFocus) Hide();
            if (slotSubMenu != null) Destroy(slotSubMenu.gameObject);
        }

        private void EnsureSlotSubMenu()
        {
            if (slotSubMenu != null) return;
            var go = new GameObject("InventorySlotSubMenu");
            go.transform.SetParent(transform, false);
            slotSubMenu = go.AddComponent<UnitActionMenuUI>();

            // Borrow the cursor/divider/font/glow material from the scene-wired
            // Warrior's Command action menu — without a font the sub-menu's TMP
            // fontMaterial reset NRE's. Then tint the panel into Indigo Codex
            // parchment so the sub-menu matches the popup that spawned it, instead
            // of inheriting Warrior's Command ember chrome.
            var template = FindAnyObjectByType<UnitActionMenuUI>(FindObjectsInactive.Include);
            if (template != null && template != slotSubMenu)
                slotSubMenu.CopyAssetsFrom(template);

            slotSubMenu.ApplyPalette(
                bgTint:       new Color(0.95f, 0.90f, 0.77f, 1f),  // parchment #f2e6c4
                textDefault:  new Color(0.24f, 0.16f, 0.10f, 1f),  // inkDeep #3d2a1a
                textSelected: new Color(0.69f, 0.22f, 0.16f, 1f),  // vermillion #b0382a
                accentBar:    new Color(0.79f, 0.60f, 0.23f, 1f)); // brass #c9993a
        }

        #region Input handling

        private void Navigate(Vector2Int dir)
        {
            if (slotSubMenuOpen) return;
            if (dir.y > 0)
                selectedIndex = selectedIndex <= 0 ? UnitInventory.Capacity - 1 : selectedIndex - 1;
            else if (dir.y < 0)
                selectedIndex = selectedIndex >= UnitInventory.Capacity - 1 ? 0 : selectedIndex + 1;
            else
                return;

            UpdateSelection();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void Confirm()
        {
            if (slotSubMenuOpen) return;
            var slot = inventory.GetSlot(selectedIndex);
            if (slot.IsEmpty) return;
            AudioManager.Instance?.Play(SoundId.ConfirmItem);
            OpenSlotSubMenu(selectedIndex, slot);
        }

        private void Cancel()
        {
            if (slotSubMenuOpen) return;
            var close = onClose;
            Hide();
            close?.Invoke();
        }

        #endregion

        private void OpenSlotSubMenu(int slotIndex, InventoryItem item)
        {
            slotSubMenuOpen = true;

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
                if (slotIndex != 0 && EquipResolver.CanEquip(unit, item.weapon))
                {
                    actions.Add("Equip");
                    handlers.Add(() =>
                    {
                        inventory.EquipFromSlot(slotIndex);
                        AudioManager.Instance?.Play(SoundId.ItemEquip);
                        ReturnToMainMenu();
                    });
                }
            }
            else if (item.kind == ItemKind.Consumable)
            {
                actions.Add("Use");
                handlers.Add(() =>
                {
                    if (item.consumable.type == ConsumableType.StatBooster && confirmDialog != null)
                    {
                        ConfirmStatBoosterUse(slotIndex, item);
                        return;
                    }

                    if (inventory.TryUseConsumable(slotIndex, out _))
                    {
                        AudioManager.Instance?.Play(SoundId.Heal);
                        var used = onConsumableUsed;
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

            slotSubMenu.Show(actions,
                onSelect: index =>
                {
                    if (index >= 0 && index < handlers.Count) handlers[index]();
                    else ReturnToMainMenu();
                },
                onCancel: ReturnToMainMenu);
        }

        private void ConfirmDiscard(int slotIndex, InventoryItem item)
        {
            if (confirmDialog == null)
            {
                inventory.DiscardSlot(slotIndex);
                ReturnToMainMenu();
                return;
            }

            confirmDialog.Show(
                $"Discard {item.DisplayName}? This cannot be undone.",
                onYes: () =>
                {
                    inventory.DiscardSlot(slotIndex);
                    ReturnToMainMenu();
                },
                onNo: ReturnToMainMenu);
        }

        private void ConfirmStatBoosterUse(int slotIndex, InventoryItem item)
        {
            var (message, _) = ConsumableEffects.DescribeStatBoost(item.consumable, unit);
            confirmDialog.Show(message,
                onYes: () =>
                {
                    if (inventory.TryUseConsumable(slotIndex, out string failReason))
                    {
                        AudioManager.Instance?.Play(SoundId.BuffApplied);
                        var used = onConsumableUsed;
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
            slotSubMenuOpen = false;
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
            if (popupInstance == null)
            {
                Debug.LogError("InventoryMenuUI: _popupInstance not wired. Run the scene setup " +
                    "menu or re-run 'Project Astra/Build Inventory Popup (prefab)'.");
                return;
            }

            if (refs == null) refs = popupInstance.GetComponent<InventoryPopupRefs>();
            if (refs == null)
            {
                Debug.LogError("InventoryMenuUI: popup instance has no InventoryPopupRefs — rebuild the prefab.");
                return;
            }

            popupInstance.SetActive(true);
            // Ensure the popup sits on top of anything else added to the canvas after setup.
            popupInstance.transform.SetAsLastSibling();

            BindUnitInfo();
            UpdateSlotLabels();
        }

        private void BindUnitInfo()
        {
            if (refs.unitName != null) refs.unitName.text = unit != null ? unit.name : "—";
            if (refs.unitClass != null) refs.unitClass.text = ClassLabel(unit);

            int hp = unit != null ? unit.currentHP : 0;
            int maxHp = unit != null ? unit.maxHP : 1;
            if (refs.hpNumbers != null) refs.hpNumbers.text = $"{hp} / {maxHp}";
            if (refs.hpFill != null)
            {
                float frac = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
                var rt = refs.hpFill.rectTransform;
                rt.sizeDelta = new Vector2(288f * frac, rt.sizeDelta.y);
            }

            if (refs.inventoryCount != null)
            {
                int occupied = inventory != null ? inventory.OccupiedCount : 0;
                refs.inventoryCount.text = $"{occupied} / {UnitInventory.Capacity}";
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
            if (refs == null) return;
            int equippedSlot = inventory != null ? inventory.EquippedWeaponSlot : -1;

            for (int i = 0; i < refs.rows.Length; i++)
            {
                var rowRefs = refs.rows[i];
                var slot = inventory != null ? inventory.GetSlot(i) : InventoryItem.None;
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
            if (item.kind == ItemKind.Consumable) return refs.sigilConsumable;
            if (item.kind != ItemKind.Weapon) return null;
            return item.weapon.weaponType switch
            {
                WeaponType.Sword => refs.sigilSword,
                WeaponType.Lance => refs.sigilLance,
                WeaponType.Axe   => refs.sigilAxe,
                WeaponType.Bow   => refs.sigilBow,
                WeaponType.Staff => refs.sigilStaff,
                _                => refs.sigilSword, // tomes fall back to sword sigil for now
            };
        }

        private void UpdateSelection()
        {
            if (refs == null) return;

            for (int i = 0; i < refs.rows.Length; i++)
            {
                var row = refs.rows[i];
                if (row == null || row.root == null) continue;

                bool selected = i == selectedIndex;
                var slot = inventory != null ? inventory.GetSlot(i) : InventoryItem.None;
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

            BindStatsPanel(inventory != null ? inventory.GetSlot(selectedIndex) : InventoryItem.None);
        }

        private void BindStatsPanel(InventoryItem item)
        {
            if (refs == null) return;

            if (item.IsEmpty)
            {
                if (refs.statsGroup != null) refs.statsGroup.alpha = 0.45f;
                refs.selectedItemName.text = "— select an item —";
                refs.selectedItemKind.text = "";
                SetStat(refs.statAtk, null);
                SetStat(refs.statHit, null);
                SetStat(refs.statRng, null);
                SetStat(refs.statWt,  null);
                refs.itemDescription.text = "";
                refs.provText.text = "";
                return;
            }

            if (refs.statsGroup != null) refs.statsGroup.alpha = 1f;
            refs.selectedItemName.text = item.DisplayName;

            if (item.kind == ItemKind.Weapon)
            {
                var w = item.weapon;
                refs.selectedItemKind.text = $"{w.weaponType.ToString().ToUpperInvariant()} · {w.tier.ToString().ToUpperInvariant()}";
                SetStat(refs.statAtk, w.might);
                SetStat(refs.statHit, w.hit);
                SetStat(refs.statRng, FormatRange(w.minRange, w.maxRange));
                SetStat(refs.statWt,  w.weight);
                refs.itemDescription.text = DescribeWeapon(w);
                refs.provText.text = w.characterLocked && !string.IsNullOrEmpty(w.ownerUnitId)
                    ? $"Bound to {w.ownerUnitId}"
                    : $"{w.minRank} rank required";
            }
            else // Consumable
            {
                var c = item.consumable;
                refs.selectedItemKind.text = c.type == ConsumableType.Vulnerary
                    ? "ELIXIR · RESTORATIVE"
                    : "STAT BOOST";
                SetStat(refs.statAtk, null);
                SetStat(refs.statHit, null);
                SetStat(refs.statRng, null);
                SetStat(refs.statWt,  null);
                refs.itemDescription.text = DescribeConsumable(c);
                refs.provText.text = $"{c.currentUses} / {c.maxUses} uses remaining";
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
