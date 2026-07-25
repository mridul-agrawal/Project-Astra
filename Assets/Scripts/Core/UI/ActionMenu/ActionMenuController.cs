using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.UI.Convoy;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Trade;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.ActionMenu
{
    public class ActionMenuController
    {
        // External References for game workflow:
        private readonly SelectionMenuView menuView;
        private readonly InventoryMenuUI inventoryMenuUI;
        private readonly ConfirmDialogUI confirmDialogUI;
        private readonly TradeScreenUI tradeUI;
        private readonly ConvoyUI convoyUI;
        private readonly ToastNotificationUI toastUI;
        private readonly TargetingFlow targetingFlow;
        private readonly StaffExecutor staffExecutor;
        private readonly GridCursor cursor;

        // Internal references for the current menu session:
        private TestUnit selectedUnit;
        private Vector2Int committedDestination;
        private ActionMenuModel currentModel;
        private List<Vector2Int> enemyTiles = new();
        private List<Vector2Int> healTiles = new();
        private List<TestUnit> adjacentAllies = new();
        private ActionChoice? lastChoice;

        // Events:
        private Action onComplete;
        private Action onCancelToUnitSelected;

        public ActionChoice? LastChoice => lastChoice;

        // Cleared by GridCursor at end of turn / unit deselect so the next selection starts fresh.
        public void ClearLastChoice() => lastChoice = null;

        public ActionMenuController(SelectionMenuView menuView, InventoryMenuUI inventoryMenuUI, ConfirmDialogUI confirmDialogUI, TradeScreenUI tradeUI,
            ConvoyUI convoyUI, ToastNotificationUI toastUI, TargetingFlow targetingFlow, StaffExecutor staffExecutor, GridCursor cursor)
        {
            this.menuView = menuView;
            this.inventoryMenuUI = inventoryMenuUI;
            this.confirmDialogUI = confirmDialogUI;
            this.tradeUI = tradeUI;
            this.convoyUI = convoyUI;
            this.toastUI = toastUI;
            this.targetingFlow = targetingFlow;
            this.staffExecutor = staffExecutor;
            this.cursor = cursor;
        }

        // Shows the action menu for the unit at its just-committed destination.
        // onComplete fires on Wait / Item-use / Supply-close.
        // onCancelToUnitSelected fires when the player cancels the menu and
        // the unit needs to be unwound back to its pre-move tile.
        public void Show(TestUnit unit, Vector2Int committedDestination, Action onComplete, Action onCancelToUnitSelected)
        {
            selectedUnit = unit;
            this.committedDestination = committedDestination;
            this.onComplete = onComplete;
            this.onCancelToUnitSelected = onCancelToUnitSelected;

            currentModel = new ActionMenuModel();
            enemyTiles = targetingFlow.GetEnemiesInAttackRange(selectedUnit, committedDestination);

            TryAddAttackAction(currentModel);
            TryAddStaffAction(currentModel);
            TryAddItemAction(currentModel);
            TryAddTradeAction(currentModel);
            TryAddSupplyAction(currentModel);

            currentModel.Add("Wait", ActionChoice.Wait);

            cursor.SetMode(ProjectAstra.Core.Cursor.CursorMode.ActionMenu);
            menuView?.Show(currentModel.ToLabels(), OnActionSelected, OnActionCancelled);
        }

        // --- Per-action eligibility ---

        private void TryAddAttackAction(ActionMenuModel model)
        {
            if (enemyTiles.Count == 0) return;
            if (selectedUnit == null || selectedUnit.Inventory.IsUnarmed) return;

            model.Add("Attack", ActionChoice.Attack);
        }

        private void TryAddStaffAction(ActionMenuModel model)
        {
            if (selectedUnit == null) return;

            var staff = selectedUnit.equippedWeapon;
            if (staff.weaponType != WeaponType.Staff) return;
            if (staff.staffEffect == StaffEffect.None || staff.IsBroken) return;

            if (staff.staffEffect == StaffEffect.AreaOfEffect)
                TryAddFortifyAction(model);
            else
                TryAddHealAction(model);
        }

        private void TryAddFortifyAction(ActionMenuModel model)
        {
            var staff = selectedUnit.equippedWeapon;
            int mag = GetMagStat(selectedUnit);
            var allUnits = UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None);

            foreach (var u in allUnits)
            {
                if (u == selectedUnit) continue;
                if (!StaffEffects.CanHealTarget(selectedUnit, u, staff, mag, out _)) continue;

                model.Add("Fortify", ActionChoice.Fortify);
                return;
            }
        }

        private void TryAddHealAction(ActionMenuModel model)
        {
            healTiles = targetingFlow.GetAlliesInHealRange(
                selectedUnit, committedDestination, GetMagStat(selectedUnit));
            if (healTiles.Count == 0) return;

            model.Add("Heal", ActionChoice.Heal);
        }

        private void TryAddItemAction(ActionMenuModel model)
        {
            if (selectedUnit == null || selectedUnit.Inventory.OccupiedCount == 0) return;

            model.Add("Item", ActionChoice.Item);
        }

        private void TryAddTradeAction(ActionMenuModel model)
        {
            adjacentAllies = selectedUnit != null
                ? AdjacentAllyFinder.FindAdjacentAllies(
                    committedDestination, Faction.Player, selectedUnit, FindUnitAt)
                : new List<TestUnit>();

            if (adjacentAllies.Count == 0) return;

            model.Add("Trade", ActionChoice.Trade);
        }

        private void TryAddSupplyAction(ActionMenuModel model)
        {
            if (selectedUnit == null || !selectedUnit.isLord) return;
            if (!ProjectAstra.Core.Convoy.Current.IsAvailable) return;

            model.Add("Supply", ActionChoice.Supply);
        }

        // --- Dispatch ---

        private void OnActionSelected(int index)
        {
            IReadOnlyList<ActionMenuEntry> entries = currentModel.Entries;
            if (index < 0 || index >= entries.Count)
            {
                lastChoice = ActionChoice.Wait;
                onComplete?.Invoke();
                return;
            }

            lastChoice = entries[index].Choice;

            switch (entries[index].Choice)
            {
                case ActionChoice.Attack: EnterAttackTargeting(); break;
                case ActionChoice.Heal: EnterHealTargeting(); break;
                case ActionChoice.Fortify: staffExecutor.TryCommitFortify(selectedUnit, onComplete); break;
                case ActionChoice.Item: OpenInventoryMenu(); break;
                case ActionChoice.Trade: ShowTradeTargetMenu(); break;
                case ActionChoice.Supply: OpenConvoyUI(); break;
                case ActionChoice.Wait:
                default: onComplete?.Invoke(); break;
            }
        }

        private void OnActionCancelled() => onCancelToUnitSelected?.Invoke();

        // Attack/Heal need _validMoveTiles in GridCursor to be set to the
        // target set before TargetingFlow takes over, because the cursor
        // movement code reads _validMoveTiles when bouncing the cursor onto
        // the first valid target. GridCursor wires _validMoveTiles via a
        // callback on the cursor's SetValidMoveTiles seam (added below).
        private void EnterAttackTargeting()
        {
            if (enemyTiles.Count == 0) { onComplete?.Invoke(); return; }
            cursor.SetValidMoveTiles(new HashSet<Vector2Int>(enemyTiles));
            targetingFlow.EnterAttackTargeting(selectedUnit, enemyTiles);
        }

        private void EnterHealTargeting()
        {
            if (healTiles.Count == 0) { ReShow(); return; }
            cursor.SetValidMoveTiles(new HashSet<Vector2Int>(healTiles));
            targetingFlow.EnterHealTargeting(selectedUnit, healTiles);
        }

        // --- Sub-action openers ---

        private void OpenInventoryMenu()
        {
            if (selectedUnit == null || inventoryMenuUI == null) { ReShow(); return; }

            inventoryMenuUI.Show(selectedUnit, confirmDialogUI,
                onConsumableUsed: () => onComplete?.Invoke(),
                onClose: ReShow);
        }

        private void ShowTradeTargetMenu()
        {
            if (adjacentAllies.Count == 1)
            {
                OpenTrade(adjacentAllies[0]);
                return;
            }

            var names = new List<string>();
            foreach (var ally in adjacentAllies)
                names.Add(ally.name);

            menuView?.Show(names,
                index => OpenTrade(adjacentAllies[index]),
                ReShow);
        }

        private void OpenTrade(TestUnit target)
        {
            if (selectedUnit == null || tradeUI == null) { ReShow(); return; }

            var session = new TradeSession(selectedUnit, target);
            tradeUI.Show(session, confirmDialogUI,
                onConfirm: ReShow,
                onCancel: ReShow);
        }

        private void OpenConvoyUI()
        {
            if (selectedUnit == null || convoyUI == null) { ReShow(); return; }

            var convoy = ProjectAstra.Core.Convoy.Current as SupplyConvoy;
            if (convoy == null) { ReShow(); return; }

            convoyUI.Show(convoy, selectedUnit, toastUI, onClose: () => onComplete?.Invoke());
        }

        // Re-opens the menu with the same callbacks; used when a sub-action
        // dialog dismisses and we want to land back on the parent menu.
        private void ReShow() => Show(selectedUnit, committedDestination, onComplete, onCancelToUnitSelected);

        // --- Static helpers ---

        private static int GetMagStat(TestUnit unit) => unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in UnityEngine.Object.FindObjectsByType<TestUnit>())
                if (unit.gridPosition == pos)
                    return unit;
            return null;
        }
    }
}
