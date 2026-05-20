using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.UI.Convoy;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Trade;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Choice the player picked from the post-movement action menu. Cached on
    // ActionMenuFlow so CantoFlow can decide later whether canto applies
    // (Wait and "no choice" don't trigger canto).
    public enum ActionChoice { Attack, Heal, Fortify, Item, Trade, Supply, Wait }

    // Builds and drives the post-movement action menu (Attack / Heal /
    // Fortify / Item / Trade / Supply / Wait). Owns:
    //   • the eligibility checks that decide which entries appear
    //   • the cached lists those checks produce (enemy / heal / adjacent-ally
    //     tiles), passed downstream to TargetingFlow / TradeSession
    //   • the dispatcher that routes the chosen entry to its handler
    //   • the sub-action UI openers (Inventory popup, Trade picker, Convoy)
    //
    // Cancel routes back through a caller-supplied callback because undoing
    // the unit's movement and restoring the UnitSelected overlay are
    // GridCursor's selection-flow concerns, not ActionMenu's.
    public class ActionMenuFlow
    {
        private readonly UnitActionMenuUI _actionMenuUI;
        private readonly InventoryMenuUI _inventoryMenuUI;
        private readonly ConfirmDialogUI _confirmDialogUI;
        private readonly TradeScreenUI _tradeUI;
        private readonly ConvoyUI _convoyUI;
        private readonly ToastNotificationUI _toastUI;
        private readonly TargetingFlow _targetingFlow;
        private readonly StaffExecutor _staffExecutor;
        private readonly GridCursor _cursor;

        private TestUnit _selectedUnit;
        private Vector2Int _committedDestination;
        private Action _onComplete;
        private Action _onCancelToUnitSelected;

        private readonly List<ActionChoice> _choices = new();
        private List<Vector2Int> _enemyTiles = new();
        private List<Vector2Int> _healTiles = new();
        private List<TestUnit> _adjacentAllies = new();
        private ActionChoice? _lastChoice;

        public ActionChoice? LastChoice => _lastChoice;

        // Cleared by GridCursor at end of turn / unit deselect so the next
        // selection starts fresh.
        public void ClearLastChoice() => _lastChoice = null;

        public ActionMenuFlow(
            UnitActionMenuUI actionMenuUI,
            InventoryMenuUI inventoryMenuUI,
            ConfirmDialogUI confirmDialogUI,
            TradeScreenUI tradeUI,
            ConvoyUI convoyUI,
            ToastNotificationUI toastUI,
            TargetingFlow targetingFlow,
            StaffExecutor staffExecutor,
            GridCursor cursor)
        {
            _actionMenuUI = actionMenuUI;
            _inventoryMenuUI = inventoryMenuUI;
            _confirmDialogUI = confirmDialogUI;
            _tradeUI = tradeUI;
            _convoyUI = convoyUI;
            _toastUI = toastUI;
            _targetingFlow = targetingFlow;
            _staffExecutor = staffExecutor;
            _cursor = cursor;
        }

        // Shows the action menu for the unit at its just-committed destination.
        // onComplete fires on Wait / Item-use / Supply-close.
        // onCancelToUnitSelected fires when the player cancels the menu and
        // the unit needs to be unwound back to its pre-move tile.
        public void Show(TestUnit unit, Vector2Int committedDestination,
            Action onComplete, Action onCancelToUnitSelected)
        {
            _selectedUnit = unit;
            _committedDestination = committedDestination;
            _onComplete = onComplete;
            _onCancelToUnitSelected = onCancelToUnitSelected;

            var labels = new List<string>();
            _choices.Clear();
            _enemyTiles = _targetingFlow.GetEnemiesInAttackRange(_selectedUnit, _committedDestination);

            TryAddAttackAction(labels);
            TryAddStaffAction(labels);
            TryAddItemAction(labels);
            TryAddTradeAction(labels);
            TryAddSupplyAction(labels);

            labels.Add("Wait");
            _choices.Add(ActionChoice.Wait);

            _cursor.SetMode(CursorMode.ActionMenu);
            _actionMenuUI?.Show(labels, OnActionSelected, OnActionCancelled);
        }

        // --- Per-action eligibility ---

        private void TryAddAttackAction(List<string> labels)
        {
            if (_enemyTiles.Count == 0) return;
            if (_selectedUnit == null || _selectedUnit.Inventory.IsUnarmed) return;

            labels.Add("Attack");
            _choices.Add(ActionChoice.Attack);
        }

        private void TryAddStaffAction(List<string> labels)
        {
            if (_selectedUnit == null) return;

            var staff = _selectedUnit.equippedWeapon;
            if (staff.weaponType != WeaponType.Staff) return;
            if (staff.staffEffect == StaffEffect.None || staff.IsBroken) return;

            if (staff.staffEffect == StaffEffect.AreaOfEffect)
                TryAddFortifyAction(labels);
            else
                TryAddHealAction(labels);
        }

        private void TryAddFortifyAction(List<string> labels)
        {
            var staff = _selectedUnit.equippedWeapon;
            int mag = GetMagStat(_selectedUnit);
            var allUnits = UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None);

            foreach (var u in allUnits)
            {
                if (u == _selectedUnit) continue;
                if (!StaffEffects.CanHealTarget(_selectedUnit, u, staff, mag, out _)) continue;

                labels.Add("Fortify");
                _choices.Add(ActionChoice.Fortify);
                return;
            }
        }

        private void TryAddHealAction(List<string> labels)
        {
            _healTiles = _targetingFlow.GetAlliesInHealRange(
                _selectedUnit, _committedDestination, GetMagStat(_selectedUnit));
            if (_healTiles.Count == 0) return;

            labels.Add("Heal");
            _choices.Add(ActionChoice.Heal);
        }

        private void TryAddItemAction(List<string> labels)
        {
            if (_selectedUnit == null || _selectedUnit.Inventory.OccupiedCount == 0) return;

            labels.Add("Item");
            _choices.Add(ActionChoice.Item);
        }

        private void TryAddTradeAction(List<string> labels)
        {
            _adjacentAllies = _selectedUnit != null
                ? AdjacentAllyFinder.FindAdjacentAllies(
                    _committedDestination, Faction.Player, _selectedUnit, FindUnitAt)
                : new List<TestUnit>();

            if (_adjacentAllies.Count == 0) return;

            labels.Add("Trade");
            _choices.Add(ActionChoice.Trade);
        }

        private void TryAddSupplyAction(List<string> labels)
        {
            if (_selectedUnit == null || !_selectedUnit.isLord) return;
            if (!Convoy.Current.IsAvailable) return;

            labels.Add("Supply");
            _choices.Add(ActionChoice.Supply);
        }

        // --- Dispatch ---

        private void OnActionSelected(int index)
        {
            if (index < 0 || index >= _choices.Count)
            {
                _lastChoice = ActionChoice.Wait;
                _onComplete?.Invoke();
                return;
            }

            _lastChoice = _choices[index];

            switch (_choices[index])
            {
                case ActionChoice.Attack: EnterAttackTargeting(); break;
                case ActionChoice.Heal: EnterHealTargeting(); break;
                case ActionChoice.Fortify: _staffExecutor.TryCommitFortify(_selectedUnit, _onComplete); break;
                case ActionChoice.Item: OpenInventoryMenu(); break;
                case ActionChoice.Trade: ShowTradeTargetMenu(); break;
                case ActionChoice.Supply: OpenConvoyUI(); break;
                case ActionChoice.Wait:
                default: _onComplete?.Invoke(); break;
            }
        }

        private void OnActionCancelled() => _onCancelToUnitSelected?.Invoke();

        // Attack/Heal need _validMoveTiles in GridCursor to be set to the
        // target set before TargetingFlow takes over, because the cursor
        // movement code reads _validMoveTiles when bouncing the cursor onto
        // the first valid target. GridCursor wires _validMoveTiles via a
        // callback on the cursor's SetValidMoveTiles seam (added below).
        private void EnterAttackTargeting()
        {
            if (_enemyTiles.Count == 0) { _onComplete?.Invoke(); return; }
            _cursor.SetValidMoveTiles(new HashSet<Vector2Int>(_enemyTiles));
            _targetingFlow.EnterAttackTargeting(_selectedUnit, _enemyTiles);
        }

        private void EnterHealTargeting()
        {
            if (_healTiles.Count == 0) { ReShow(); return; }
            _cursor.SetValidMoveTiles(new HashSet<Vector2Int>(_healTiles));
            _targetingFlow.EnterHealTargeting(_selectedUnit, _healTiles);
        }

        // --- Sub-action openers ---

        private void OpenInventoryMenu()
        {
            if (_selectedUnit == null || _inventoryMenuUI == null) { ReShow(); return; }

            _inventoryMenuUI.Show(_selectedUnit, _confirmDialogUI,
                onConsumableUsed: () => _onComplete?.Invoke(),
                onClose: ReShow);
        }

        private void ShowTradeTargetMenu()
        {
            if (_adjacentAllies.Count == 1)
            {
                OpenTrade(_adjacentAllies[0]);
                return;
            }

            var names = new List<string>();
            foreach (var ally in _adjacentAllies)
                names.Add(ally.name);

            _actionMenuUI?.Show(names,
                index => OpenTrade(_adjacentAllies[index]),
                ReShow);
        }

        private void OpenTrade(TestUnit target)
        {
            if (_selectedUnit == null || _tradeUI == null) { ReShow(); return; }

            var session = new TradeSession(_selectedUnit, target);
            _tradeUI.Show(session, _confirmDialogUI,
                onConfirm: ReShow,
                onCancel: ReShow);
        }

        private void OpenConvoyUI()
        {
            if (_selectedUnit == null || _convoyUI == null) { ReShow(); return; }

            var convoy = Convoy.Current as SupplyConvoy;
            if (convoy == null) { ReShow(); return; }

            _convoyUI.Show(convoy, _selectedUnit, _toastUI, onClose: () => _onComplete?.Invoke());
        }

        // Re-opens the menu with the same callbacks; used when a sub-action
        // dialog dismisses and we want to land back on the parent menu.
        private void ReShow() => Show(_selectedUnit, _committedDestination, _onComplete, _onCancelToUnitSelected);

        // --- Static helpers ---

        private static int GetMagStat(TestUnit unit) =>
            unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == pos)
                    return unit;
            return null;
        }
    }
}
