using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
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

        TradeSession _session;
        ConfirmDialogUI _confirmDialog;
        Action _onConfirm;
        Action _onCancel;

        Column _activeColumn;
        int _cursorRow;
        Phase _phase;
        Column _selectedColumn;
        int _selectedRow;

        GameObject _dimOverlay;

        // Per-side panel refs resolved once on first Show().
        bool _refsDiscovered;
        Transform[] _leftRows;
        Transform[] _rightRows;
        TextMeshProUGUI _leftName;
        TextMeshProUGUI _rightName;
        TextMeshProUGUI _leftEpithet;
        TextMeshProUGUI _rightEpithet;
        TextMeshProUGUI _leftStatsLine;
        TextMeshProUGUI _rightStatsLine;
        TextMeshProUGUI _leftPortraitLabel;
        TextMeshProUGUI _rightPortraitLabel;
        TextMeshProUGUI _holdingValue;

        GameObject _tooltipRoot;
        TextMeshProUGUI _tooltipName;
        TextMeshProUGUI _tooltipType;
        TextMeshProUGUI _tooltipStats;
        TextMeshProUGUI _tooltipEffectiveness;
        TextMeshProUGUI _tooltipDescription;

        public void Show(TradeSession session, ConfirmDialogUI confirmDialog,
            Action onConfirm, Action onCancel)
        {
            if (session == null) return;

            _session = session;
            _confirmDialog = confirmDialog;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _activeColumn = Column.Left;
            _cursorRow = 0;
            _phase = Phase.Browsing;

            if (!_refsDiscovered) DiscoverReferences();

            gameObject.SetActive(true);
            if (_dimOverlay != null) _dimOverlay.SetActive(true);

            PopulateHeaders();
            RefreshAllRows();
            UpdateVisuals();

            HasInputFocus = true;
            SubscribeInput();
        }

        public void Hide()
        {
            HasInputFocus = false;
            UnsubscribeInput();

            gameObject.SetActive(false);
            if (_dimOverlay != null) _dimOverlay.SetActive(false);

            _session = null;
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
            _refsDiscovered = true;

            var parent = transform.parent;
            if (parent != null)
            {
                var overlay = parent.Find("TradeScreenDimOverlay");
                if (overlay != null) _dimOverlay = overlay.gameObject;
            }

            _leftRows  = ResolveRows("LeftPanel");
            _rightRows = ResolveRows("RightPanel");

            _leftName         = FindTMP("LeftPanel/Content/InfoBlock/UnitName");
            _rightName        = FindTMP("RightPanel/Content/InfoBlock/UnitName");
            _leftEpithet      = FindTMP("LeftPanel/Content/InfoBlock/Epithet");
            _rightEpithet     = FindTMP("RightPanel/Content/InfoBlock/Epithet");
            _leftStatsLine    = FindTMP("LeftPanel/Content/InfoBlock/StatsLine");
            _rightStatsLine   = FindTMP("RightPanel/Content/InfoBlock/StatsLine");
            _leftPortraitLabel  = FindTMP("LeftPanel/Content/Portrait/PortraitLabel");
            _rightPortraitLabel = FindTMP("RightPanel/Content/Portrait/PortraitLabel");
            _holdingValue     = FindTMP("ActionBar/HoldingReadout/HoldingValue");

            var tooltip = FindChildDeep(transform, "ItemTooltip");
            if (tooltip != null)
            {
                _tooltipRoot = tooltip.gameObject;
                _tooltipName          = tooltip.Find("Name")?.GetComponent<TextMeshProUGUI>();
                _tooltipType          = tooltip.Find("Type")?.GetComponent<TextMeshProUGUI>();
                _tooltipStats         = tooltip.Find("Stats")?.GetComponent<TextMeshProUGUI>();
                _tooltipEffectiveness = tooltip.Find("Effectiveness")?.GetComponent<TextMeshProUGUI>();
                _tooltipDescription   = tooltip.Find("Description")?.GetComponent<TextMeshProUGUI>();
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
            PopulateUnitHeader(_session.LeftUnit,  _leftName,  _leftEpithet,  _leftStatsLine,  _leftPortraitLabel);
            PopulateUnitHeader(_session.RightUnit, _rightName, _rightEpithet, _rightStatsLine, _rightPortraitLabel);
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
            RefreshSide(_leftRows,  isLeft: true);
            RefreshSide(_rightRows, isLeft: false);
        }

        void RefreshSide(Transform[] rows, bool isLeft)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                var item = isLeft ? _session.GetLeftSlot(i) : _session.GetRightSlot(i);
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
                _cursorRow = _cursorRow <= 0 ? TradeSession.Capacity - 1 : _cursorRow - 1;
            else if (dir.y < 0)
                _cursorRow = _cursorRow >= TradeSession.Capacity - 1 ? 0 : _cursorRow + 1;
            else if (dir.x != 0)
                _activeColumn = _activeColumn == Column.Left ? Column.Right : Column.Left;
            else
                return;

            UpdateVisuals();
        }

        void Confirm()
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

            // Phase.ItemSelected
            if (_activeColumn == _selectedColumn)
            {
                // Second confirm on the same column cancels the selection.
                Deselect();
                return;
            }

            bool success = ExecuteTradeOperation();
            Deselect();
            if (success) RefreshAllRows();
            UpdateVisuals();
        }

        void Cancel()
        {
            if (_phase == Phase.ItemSelected)
            {
                Deselect();
                return;
            }

            if (_session == null || !_session.HasChanges)
            {
                var cb = _onCancel;
                Hide();
                cb?.Invoke();
                return;
            }

            // Suspend input while the confirm dialog is open.
            HasInputFocus = false;
            UnsubscribeInput();

            if (_confirmDialog == null)
            {
                // No dialog wired: commit silently so in-progress trades don't get lost.
                _session.Commit();
                var cb = _onConfirm;
                Hide();
                cb?.Invoke();
                return;
            }

            _confirmDialog.Show("Apply trade changes?",
                onYes: () =>
                {
                    _session.Commit();
                    var cb = _onConfirm;
                    Hide();
                    cb?.Invoke();
                },
                onNo: () =>
                {
                    var cb = _onCancel;
                    Hide();
                    cb?.Invoke();
                });
        }

        void Deselect()
        {
            _phase = Phase.Browsing;
            UpdateVisuals();
        }

        // ================================================================
        // Session operations
        // ================================================================

        bool ExecuteTradeOperation()
        {
            if (_selectedColumn == Column.Left && _activeColumn == Column.Right)
            {
                var rightItem = _session.GetRightSlot(_cursorRow);
                return rightItem.IsEmpty
                    ? _session.TryGive(_selectedRow)
                    : _session.TrySwap(_selectedRow, _cursorRow);
            }

            if (_selectedColumn == Column.Right && _activeColumn == Column.Left)
            {
                var leftItem = _session.GetLeftSlot(_cursorRow);
                return leftItem.IsEmpty
                    ? _session.TryTake(_selectedRow)
                    : _session.TrySwap(_cursorRow, _selectedRow);
            }

            return false;
        }

        InventoryItem GetSlotAtCursor()
        {
            return _activeColumn == Column.Left
                ? _session.GetLeftSlot(_cursorRow)
                : _session.GetRightSlot(_cursorRow);
        }

        // ================================================================
        // Visuals
        // ================================================================

        void UpdateVisuals()
        {
            UpdateSideVisuals(_leftRows,  Column.Left);
            UpdateSideVisuals(_rightRows, Column.Right);
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
                bool isCursor = col == _activeColumn && i == _cursorRow;
                bool isSelected = _phase == Phase.ItemSelected
                    && col == _selectedColumn
                    && i == _selectedRow;

                var item = col == Column.Left ? _session.GetLeftSlot(i) : _session.GetRightSlot(i);
                bool disabled = TradeScreenRowVisuals.IsDisabled(item);

                if (isSelected) vs = TradeRowVisualState.Selected;
                else if (isCursor && _phase == Phase.ItemSelected) vs = TradeRowVisualState.Pressed;
                else if (isCursor) vs = TradeRowVisualState.Focused;
                else if (disabled) vs = TradeRowVisualState.Disabled;
                else vs = TradeRowVisualState.Default;

                TradeScreenRowVisuals.SetRowState(rows[i], vs);
                TradeScreenRowVisuals.ApplyTextEmphasis(rows[i], vs);
            }
        }

        void UpdateHoldingText()
        {
            if (_holdingValue == null) return;

            if (_phase == Phase.ItemSelected)
            {
                var heldItem = _selectedColumn == Column.Left
                    ? _session.GetLeftSlot(_selectedRow)
                    : _session.GetRightSlot(_selectedRow);
                if (!heldItem.IsEmpty)
                {
                    string uses = heldItem.Indestructible ? "\u221E" : heldItem.CurrentUses.ToString();
                    _holdingValue.text = heldItem.DisplayName + "  \u00B7  " + uses + " uses";
                    return;
                }
            }

            var cursorItem = GetSlotAtCursor();
            if (cursorItem.IsEmpty)
            {
                _holdingValue.text = "—";
                return;
            }

            string u = cursorItem.Indestructible ? "\u221E" : cursorItem.CurrentUses.ToString();
            _holdingValue.text = cursorItem.DisplayName + "  \u00B7  " + u + " uses";
        }

        void UpdateTooltip()
        {
            if (_tooltipRoot == null) return;

            var item = GetSlotAtCursor();
            if (item.IsEmpty)
            {
                _tooltipRoot.SetActive(false);
                return;
            }

            _tooltipRoot.SetActive(true);
            PopulateTooltip(item);
        }

        void PopulateTooltip(InventoryItem item)
        {
            bool isWeapon = item.kind == ItemKind.Weapon;
            bool isConsumable = item.kind == ItemKind.Consumable;

            if (_tooltipName != null) _tooltipName.text = item.DisplayName;

            if (_tooltipType != null)
            {
                string typeStr = isWeapon ? item.weapon.weaponType.ToString()
                    : isConsumable ? "Consumable"
                    : "Item";
                _tooltipType.text = typeStr.ToUpper();
            }

            if (_tooltipStats != null)
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
                    _tooltipStats.text = sb.ToString();
                }
                else if (isConsumable)
                {
                    var c = item.consumable;
                    string uses = c.currentUses + " / " + c.maxUses;
                    _tooltipStats.text = "<color=#e8c66a>Uses </color>" + uses;
                }
                else _tooltipStats.text = "";
            }

            if (_tooltipEffectiveness != null)
            {
                if (isWeapon && item.weapon.effectivenessTargets != null && item.weapon.effectivenessTargets.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var t in item.weapon.effectivenessTargets)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append("Effective vs. ").Append(t);
                    }
                    _tooltipEffectiveness.text = sb.ToString();
                    _tooltipEffectiveness.gameObject.SetActive(true);
                }
                else
                {
                    _tooltipEffectiveness.gameObject.SetActive(false);
                }
            }

            if (_tooltipDescription != null)
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
                _tooltipDescription.text = desc;
                _tooltipDescription.gameObject.SetActive(!string.IsNullOrEmpty(desc));
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
