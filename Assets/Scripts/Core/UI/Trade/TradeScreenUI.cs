using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Trade
{
    // Runtime controller for the Indigo Codex trade screen built by TradeScreenBuilder.
    //
    // Drops in as a replacement for the earlier placeholder TradeUI. Drives the full
    // visual layout (panels, rows, tooltip, action bar) from a TradeSession and
    // commits changes back to the underlying UnitInventories when the player confirms.
    //
    // Scene setup: run "Project Astra/Build Trade Screen (runtime)" once. That creates
    // the canvas hierarchy, attaches this component to the TradeScreen root, and
    // auto-wires GridCursor._tradeUI. The screen starts inactive and Show() toggles
    // it on when Trade is selected from the unit action menu.
    public class TradeScreenUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        enum Column { Left, Right }
        enum Phase { Browsing, ItemSelected }

        TradeSession session;
        ConfirmDialogUI confirmDialog;
        Action onConfirm;
        Action onCancel;

        Column activeColumn;
        int cursorRow;
        Phase phase;
        Column selectedColumn;
        int selectedRow;

        GameObject dimOverlay;

        // Per-side panel refs resolved once on first Show().
        bool refsDiscovered;
        Transform[] leftRows;
        Transform[] rightRows;
        TextMeshProUGUI leftName;
        TextMeshProUGUI rightName;
        TextMeshProUGUI leftEpithet;
        TextMeshProUGUI rightEpithet;
        TextMeshProUGUI leftStatsLine;
        TextMeshProUGUI rightStatsLine;
        TextMeshProUGUI leftPortraitLabel;
        TextMeshProUGUI rightPortraitLabel;
        TextMeshProUGUI holdingValue;

        GameObject tooltipRoot;
        TextMeshProUGUI tooltipName;
        TextMeshProUGUI tooltipType;
        TextMeshProUGUI tooltipStats;
        TextMeshProUGUI tooltipEffectiveness;
        TextMeshProUGUI tooltipDescription;

        public void Show(TradeSession session, ConfirmDialogUI confirmDialog,
            Action onConfirm, Action onCancel)
        {
            if (session == null) return;

            this.session = session;
            this.confirmDialog = confirmDialog;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            activeColumn = Column.Left;
            cursorRow = 0;
            phase = Phase.Browsing;

            if (!refsDiscovered) DiscoverReferences();

            gameObject.SetActive(true);
            if (dimOverlay != null) dimOverlay.SetActive(true);

            PopulateHeaders();
            RefreshAllRows();
            UpdateVisuals();

            HasInputFocus = true;
            SubscribeInput();
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        public void Hide()
        {
            bool wasOpen = HasInputFocus;
            HasInputFocus = false;
            UnsubscribeInput();

            gameObject.SetActive(false);
            if (dimOverlay != null) dimOverlay.SetActive(false);

            session = null;
            if (wasOpen) AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        void OnDestroy()
        {
            if (HasInputFocus) Hide();
        }

        // ================================================================
        // Reference discovery
        // ================================================================

        void DiscoverReferences()
        {
            refsDiscovered = true;

            var parent = transform.parent;
            if (parent != null)
            {
                var overlay = parent.Find("TradeScreenDimOverlay");
                if (overlay != null) dimOverlay = overlay.gameObject;
            }

            leftRows  = ResolveRows("LeftPanel");
            rightRows = ResolveRows("RightPanel");

            leftName         = FindTMP("LeftPanel/Content/InfoBlock/UnitName");
            rightName        = FindTMP("RightPanel/Content/InfoBlock/UnitName");
            leftEpithet      = FindTMP("LeftPanel/Content/InfoBlock/Epithet");
            rightEpithet     = FindTMP("RightPanel/Content/InfoBlock/Epithet");
            leftStatsLine    = FindTMP("LeftPanel/Content/InfoBlock/StatsLine");
            rightStatsLine   = FindTMP("RightPanel/Content/InfoBlock/StatsLine");
            leftPortraitLabel  = FindTMP("LeftPanel/Content/Portrait/PortraitLabel");
            rightPortraitLabel = FindTMP("RightPanel/Content/Portrait/PortraitLabel");
            holdingValue     = FindTMP("ActionBar/HoldingReadout/HoldingValue");

            var tooltip = FindChildDeep(transform, "ItemTooltip");
            if (tooltip != null)
            {
                tooltipRoot = tooltip.gameObject;
                tooltipName          = tooltip.Find("Name")?.GetComponent<TextMeshProUGUI>();
                tooltipType          = tooltip.Find("Type")?.GetComponent<TextMeshProUGUI>();
                tooltipStats         = tooltip.Find("Stats")?.GetComponent<TextMeshProUGUI>();
                tooltipEffectiveness = tooltip.Find("Effectiveness")?.GetComponent<TextMeshProUGUI>();
                tooltipDescription   = tooltip.Find("Description")?.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform[] ResolveRows(string panelName)
        {
            var rows = new Transform[TradeSession.Capacity];
            var rowsRoot = FindChildDeepPath(transform, panelName + "/Content/InventoryRows");
            if (rowsRoot == null) return rows;
            for (int i = 0; i < TradeSession.Capacity; i++)
                rows[i] = rowsRoot.Find("Row_" + i);
            return rows;
        }

        TextMeshProUGUI FindTMP(string path)
        {
            var t = FindChildDeepPath(transform, path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        // ================================================================
        // Headers / rows
        // ================================================================

        void PopulateHeaders()
        {
            PopulateUnitHeader(session.LeftUnit,  leftName,  leftEpithet,  leftStatsLine,  leftPortraitLabel);
            PopulateUnitHeader(session.RightUnit, rightName, rightEpithet, rightStatsLine, rightPortraitLabel);
        }

        static void PopulateUnitHeader(TestUnit unit,
            TextMeshProUGUI nameT, TextMeshProUGUI epithetT,
            TextMeshProUGUI statsT, TextMeshProUGUI portraitT)
        {
            if (unit == null) return;

            var inst = unit.UnitInstance;
            var def  = inst?.Definition;
            string displayName = (def != null && !string.IsNullOrEmpty(def.UnitName))
                ? def.UnitName
                : unit.gameObject.name;

            if (nameT != null) nameT.text = displayName;

            if (portraitT != null)
                portraitT.text = "[ portrait: " + displayName + " ]";

            if (epithetT != null)
            {
                // Prefer the unit's class label as a quick-read epithet — e.g. "ARCHER".
                var cls = inst?.CurrentClass;
                epithetT.text = cls != null ? cls.ClassName.ToUpper() : "";
            }

            if (statsT != null)
            {
                int level = inst?.Level ?? 1;
                string className = inst?.CurrentClass?.ClassName ?? "—";
                int occupied = unit.Inventory != null ? unit.Inventory.OccupiedCount : 0;
                statsT.text =
                    "<color=#e8c66a>CLASS </color>" + className +
                    "   <color=#e8c66a>LV </color>" + level +
                    "   <color=#e8c66a>CARRY </color>" + occupied + "/" + UnitInventory.Capacity;
            }
        }

        void RefreshAllRows()
        {
            RefreshSide(leftRows,  isLeft: true);
            RefreshSide(rightRows, isLeft: false);
        }

        void RefreshSide(Transform[] rows, bool isLeft)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                var item = isLeft ? session.GetLeftSlot(i) : session.GetRightSlot(i);
                TradeScreenRowVisuals.SetRowItem(rows[i], item, isLeft, TradeScreenRowVisuals.IsDisabled(item));
            }
        }

        // ================================================================
        // Input
        // ================================================================

        void SubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += Confirm;
            InputManager.Instance.OnCancel += Cancel;
        }

        void UnsubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= Confirm;
            InputManager.Instance.OnCancel -= Cancel;
        }

        void Navigate(Vector2Int dir)
        {
            if (dir.y > 0)
                cursorRow = cursorRow <= 0 ? TradeSession.Capacity - 1 : cursorRow - 1;
            else if (dir.y < 0)
                cursorRow = cursorRow >= TradeSession.Capacity - 1 ? 0 : cursorRow + 1;
            else if (dir.x != 0)
                activeColumn = activeColumn == Column.Left ? Column.Right : Column.Left;
            else
                return;

            UpdateVisuals();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        void Confirm()
        {
            if (phase == Phase.Browsing)
            {
                var item = GetSlotAtCursor();
                if (item.IsEmpty) return;

                AudioManager.Instance?.Play(SoundId.ConfirmItem);
                phase = Phase.ItemSelected;
                selectedColumn = activeColumn;
                selectedRow = cursorRow;
                UpdateVisuals();
                return;
            }

            // Phase.ItemSelected
            if (activeColumn == selectedColumn)
            {
                // Second confirm on the same column cancels the selection.
                Deselect();
                return;
            }

            bool success = ExecuteTradeOperation();
            AudioManager.Instance?.Play(SoundId.ItemMove);
            Deselect();
            if (success) RefreshAllRows();
            UpdateVisuals();
        }

        void Cancel()
        {
            if (phase == Phase.ItemSelected)
            {
                Deselect();
                return;
            }

            if (session == null || !session.HasChanges)
            {
                var cb = onCancel;
                Hide();
                cb?.Invoke();
                return;
            }

            // Suspend input while the confirm dialog is open.
            HasInputFocus = false;
            UnsubscribeInput();

            if (confirmDialog == null)
            {
                // No dialog wired: commit silently so in-progress trades don't get lost.
                session.Commit();
                var cb = onConfirm;
                Hide();
                cb?.Invoke();
                return;
            }

            confirmDialog.Show("Apply trade changes?",
                onYes: () =>
                {
                    session.Commit();
                    var cb = onConfirm;
                    Hide();
                    cb?.Invoke();
                },
                onNo: () =>
                {
                    var cb = onCancel;
                    Hide();
                    cb?.Invoke();
                });
        }

        void Deselect()
        {
            phase = Phase.Browsing;
            UpdateVisuals();
        }

        // ================================================================
        // Session operations
        // ================================================================

        bool ExecuteTradeOperation()
        {
            if (selectedColumn == Column.Left && activeColumn == Column.Right)
            {
                var rightItem = session.GetRightSlot(cursorRow);
                return rightItem.IsEmpty
                    ? session.TryGive(selectedRow)
                    : session.TrySwap(selectedRow, cursorRow);
            }

            if (selectedColumn == Column.Right && activeColumn == Column.Left)
            {
                var leftItem = session.GetLeftSlot(cursorRow);
                return leftItem.IsEmpty
                    ? session.TryTake(selectedRow)
                    : session.TrySwap(cursorRow, selectedRow);
            }

            return false;
        }

        InventoryItem GetSlotAtCursor()
        {
            return activeColumn == Column.Left
                ? session.GetLeftSlot(cursorRow)
                : session.GetRightSlot(cursorRow);
        }

        // ================================================================
        // Visuals
        // ================================================================

        void UpdateVisuals()
        {
            UpdateSideVisuals(leftRows,  Column.Left);
            UpdateSideVisuals(rightRows, Column.Right);
            UpdateTooltip();
            UpdateHoldingText();
        }

        void UpdateSideVisuals(Transform[] rows, Column col)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;

                TradeRowVisualState vs;
                bool isCursor = col == activeColumn && i == cursorRow;
                bool isSelected = phase == Phase.ItemSelected
                    && col == selectedColumn
                    && i == selectedRow;

                var item = col == Column.Left ? session.GetLeftSlot(i) : session.GetRightSlot(i);
                bool disabled = TradeScreenRowVisuals.IsDisabled(item);

                if (isSelected) vs = TradeRowVisualState.Selected;
                else if (isCursor && phase == Phase.ItemSelected) vs = TradeRowVisualState.Pressed;
                else if (isCursor) vs = TradeRowVisualState.Focused;
                else if (disabled) vs = TradeRowVisualState.Disabled;
                else vs = TradeRowVisualState.Default;

                TradeScreenRowVisuals.SetRowState(rows[i], vs);
                TradeScreenRowVisuals.ApplyTextEmphasis(rows[i], vs);
            }
        }

        void UpdateHoldingText()
        {
            if (holdingValue == null) return;

            if (phase == Phase.ItemSelected)
            {
                var heldItem = selectedColumn == Column.Left
                    ? session.GetLeftSlot(selectedRow)
                    : session.GetRightSlot(selectedRow);
                if (!heldItem.IsEmpty)
                {
                    string uses = heldItem.Indestructible ? "\u221E" : heldItem.CurrentUses.ToString();
                    holdingValue.text = heldItem.DisplayName + "  \u00B7  " + uses + " uses";
                    return;
                }
            }

            var cursorItem = GetSlotAtCursor();
            if (cursorItem.IsEmpty)
            {
                holdingValue.text = "—";
                return;
            }

            string u = cursorItem.Indestructible ? "\u221E" : cursorItem.CurrentUses.ToString();
            holdingValue.text = cursorItem.DisplayName + "  \u00B7  " + u + " uses";
        }

        void UpdateTooltip()
        {
            if (tooltipRoot == null) return;

            var item = GetSlotAtCursor();
            if (item.IsEmpty)
            {
                tooltipRoot.SetActive(false);
                return;
            }

            tooltipRoot.SetActive(true);
            PopulateTooltip(item);
        }

        void PopulateTooltip(InventoryItem item)
        {
            bool isWeapon = item.kind == ItemKind.Weapon;
            bool isConsumable = item.kind == ItemKind.Consumable;

            if (tooltipName != null) tooltipName.text = item.DisplayName;

            if (tooltipType != null)
            {
                string typeStr = isWeapon ? item.weapon.weaponType.ToString()
                    : isConsumable ? "Consumable"
                    : "Item";
                tooltipType.text = typeStr.ToUpper();
            }

            if (tooltipStats != null)
            {
                if (isWeapon)
                {
                    var w = item.weapon;
                    string range = w.minRange == w.maxRange
                        ? w.minRange.ToString()
                        : w.minRange + "\u2013" + w.maxRange;
                    var sb = new StringBuilder();
                    sb.Append("<color=#e8c66a>Mt </color>").Append(w.might);
                    sb.Append("   <color=#e8c66a>Hit </color>").Append(w.hit);
                    sb.Append("   <color=#e8c66a>Crit </color>").Append(w.crit);
                    sb.Append("   <color=#e8c66a>Wt </color>").Append(w.weight);
                    sb.Append("   <color=#e8c66a>Rng </color>").Append(range);
                    sb.Append("   <color=#e8c66a>Rank </color>").Append(w.minRank);
                    string uses = w.indestructible ? "\u221E" : w.currentUses + " / " + w.maxUses;
                    sb.Append("   <color=#e8c66a>Uses </color>").Append(uses);
                    tooltipStats.text = sb.ToString();
                }
                else if (isConsumable)
                {
                    var c = item.consumable;
                    string uses = c.currentUses + " / " + c.maxUses;
                    tooltipStats.text = "<color=#e8c66a>Uses </color>" + uses;
                }
                else tooltipStats.text = "";
            }

            if (tooltipEffectiveness != null)
            {
                if (isWeapon && item.weapon.effectivenessTargets != null && item.weapon.effectivenessTargets.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var t in item.weapon.effectivenessTargets)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append("Effective vs. ").Append(t);
                    }
                    tooltipEffectiveness.text = sb.ToString();
                    tooltipEffectiveness.gameObject.SetActive(true);
                }
                else
                {
                    tooltipEffectiveness.gameObject.SetActive(false);
                }
            }

            if (tooltipDescription != null)
            {
                string desc = "";
                if (isWeapon)
                {
                    var w = item.weapon;
                    var sb = new StringBuilder();
                    if (w.brave) sb.Append("Brave — attacks twice. ");
                    if (!string.IsNullOrEmpty(desc)) sb.Append(desc);
                    desc = sb.ToString().Trim();
                }
                else if (isConsumable)
                {
                    var c = item.consumable;
                    desc = c.type switch
                    {
                        ConsumableType.Vulnerary   => "Restores " + c.magnitude + " HP.",
                        ConsumableType.StatBooster => "+" + c.magnitude + " " + c.targetStat + " permanently.",
                        _ => "",
                    };
                }
                tooltipDescription.text = desc;
                tooltipDescription.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            }
        }

        // ================================================================
        // Hierarchy helpers
        // ================================================================

        static Transform FindChildDeep(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
                var found = FindChildDeep(c, name);
                if (found != null) return found;
            }
            return null;
        }

        static Transform FindChildDeepPath(Transform root, string path)
        {
            var parts = path.Split('/');
            Transform current = FindChildDeep(root, parts[0]);
            if (current == null) return null;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null) return null;
            }
            return current;
        }
    }
}
