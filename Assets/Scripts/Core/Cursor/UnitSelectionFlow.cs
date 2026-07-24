using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Owns the player's unit-selection lifecycle: picking a unit at the
    // cursor's tile, computing where it can reach, constraining the cursor
    // to the reachable set, committing a movement destination, showing the
    // post-movement action menu, and finalizing the unit's turn.
    //
    // Selection-flow state (selected unit, reachability, valid move tiles,
    // memorized cursor position) lives here. Other flows (TargetingFlow,
    // ActionMenuFlow, CantoFlow) read or mutate it through this class's
    // public API rather than holding their own copies.
    public class UnitSelectionFlow
    {
        private readonly PathfindingService pathfindingService;
        private readonly UnitMover unitMover;
        private readonly RangeHighlighter rangeHighlighter;
        private readonly PathArrowRenderer pathArrowRenderer;
        private readonly CombatForecastUIController combatForecastUI;
        private readonly GridCursor cursor;
        private readonly CantoFlow cantoFlow;
        private readonly ActionMenuFlow actionMenuFlow;

        private TestUnit selectedUnit;
        private Pathfinder.ReachabilityResult currentReachability;
        private Vector2Int committedDestination;
        private HashSet<Vector2Int> validMoveTiles;
        private Vector2Int? memorizedPosition;
        private Dictionary<Vector2Int, Pathfinder.OccupantType> occupancySnapshot;

        // --- Read-only accessors (consumed by GridCursor + other flows + tests) ---

        public TestUnit SelectedUnit => selectedUnit;
        public Vector2Int CommittedDestination => committedDestination;
        public Pathfinder.ReachabilityResult CurrentReachability => currentReachability;
        public HashSet<Vector2Int> ValidMoveTiles => validMoveTiles;
        public bool IsMovementConstrained => validMoveTiles != null;

        public UnitSelectionFlow(
            PathfindingService pathfindingService,
            UnitMover unitMover,
            RangeHighlighter rangeHighlighter,
            PathArrowRenderer pathArrowRenderer,
            CombatForecastUIController combatForecastUI,
            GridCursor cursor,
            CantoFlow cantoFlow,
            ActionMenuFlow actionMenuFlow)
        {
            this.pathfindingService = pathfindingService;
            this.unitMover = unitMover;
            this.rangeHighlighter = rangeHighlighter;
            this.pathArrowRenderer = pathArrowRenderer;
            this.combatForecastUI = combatForecastUI;
            this.cursor = cursor;
            this.cantoFlow = cantoFlow;
            this.actionMenuFlow = actionMenuFlow;
        }

        // --- Entry points (HandleConfirm dispatches here) ---

        public void TrySelectUnit(Vector2Int cursorPos)
        {
            TestUnit unit = FindUnitAt(cursorPos);
            if (!IsUnitSelectable(unit)) return;

            selectedUnit = unit;
            EnterUnitSelectedMode();
        }

        public void TryCommitMovement(Vector2Int destination)
        {
            if (!currentReachability.Destinations.Contains(destination)) return;

            committedDestination = destination;
            selectedUnit.preMovementPosition = selectedUnit.gridPosition;

            var path = Pathfinder.ReconstructPath(selectedUnit.gridPosition, committedDestination, currentReachability);

            ClearOverlay();
            cursor.SetMode(CursorMode.Locked);

            if (PathExists(path))
                unitMover.MoveAlongPath(selectedUnit, path, OnMovementComplete);
            else
                OnMovementComplete();
        }

        // --- Cursor-driven (HandleCursorMove on UnitSelected) ---

        public void UpdatePathArrow(Vector2Int cursorPos)
        {
            if (pathArrowRenderer == null || selectedUnit == null) return;

            if (!currentReachability.Destinations.Contains(cursorPos))
            {
                pathArrowRenderer.Clear();
                return;
            }

            var path = Pathfinder.ReconstructPath(selectedUnit.gridPosition, cursorPos, currentReachability);
            pathArrowRenderer.ShowPath(path);
        }

        // --- Mutation seams used by ActionMenuFlow + CantoFlow ---

        public void SetValidMoveTiles(HashSet<Vector2Int> tiles) => validMoveTiles = tiles;

        // CantoFlow: ensures the unit's current tile counts as a legal
        // "stay put" exit even when reachability would exclude it.
        public void EnsureMoveTileAllowed(Vector2Int tile)
        {
            if (validMoveTiles != null && !validMoveTiles.Contains(tile))
                validMoveTiles.Add(tile);
        }

        // Called by CantoFlow at canto entry and by TrySelectUnit on initial
        // selection. Recomputes reachability against the unit's CURRENT
        // movement points (which canto has just reduced).
        public void EnterUnitSelectedMode()
        {
            if (pathfindingService == null || selectedUnit == null) return;

            occupancySnapshot = BuildOccupancySnapshot();
            currentReachability = pathfindingService.ComputeReachability(
                selectedUnit.gridPosition, selectedUnit.movementPoints,
                selectedUnit.movementType, GetOccupantType);

            RestoreMovementConstraintsAndOverlay();
            memorizedPosition = cursor.GridPosition;
            cursor.SetPosition(selectedUnit.gridPosition);
            cursor.SetMode(CursorMode.UnitSelected);
        }

        private Pathfinder.OccupantType GetOccupantType(Vector2Int pos) =>
            occupancySnapshot != null && occupancySnapshot.TryGetValue(pos, out var occupant)
                ? occupant
                : Pathfinder.OccupantType.None;

        // Snapshotted once per selection (movement is sequential, so positions can't
        // change mid-selection) — Dijkstra queries per tile, a live scan would be O(tiles × units).
        private Dictionary<Vector2Int, Pathfinder.OccupantType> BuildOccupancySnapshot()
        {
            var snapshot = new Dictionary<Vector2Int, Pathfinder.OccupantType>();
            foreach (var unit in UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unit == selectedUnit) continue;
                snapshot[unit.gridPosition] = FactionOf(unit) == Faction.Enemy
                    ? Pathfinder.OccupantType.Enemy
                    : Pathfinder.OccupantType.Ally;
            }
            return snapshot;
        }

        private static Faction FactionOf(TestUnit unit) =>
            TurnManager.Instance?.UnitRegistry.GetFaction(unit) ?? unit.faction;

        public void RestoreMovementConstraintsAndOverlay()
        {
            validMoveTiles = new HashSet<Vector2Int>(currentReachability.Destinations);
            validMoveTiles.UnionWith(currentReachability.PassThrough);
            rangeHighlighter?.ShowMovementRange(currentReachability.Destinations, currentReachability.PassThrough);
        }

        // --- Cancel/cleanup paths ---

        // Called from GridCursor.OnActionCancelled (action menu's Cancel
        // button). Hides any leftover forecast, undoes the unit's movement,
        // and returns the cursor to UnitSelected at the unit's pre-move tile.
        // Used by HandleCancel.UnitSelected when the user backs out during
        // canto's second move-only round. Clears overlay, restores the
        // unit's full MP via CantoFlow, and finalizes the turn.
        public void FinishCantoFromCancel()
        {
            ClearOverlay();
            cantoFlow.FinalizeCanto(selectedUnit);
            FinishTurn();
        }

        public void RestoreFromActionCancel()
        {
            combatForecastUI?.Hide();

            if (unitMover != null)
                unitMover.UndoMove(selectedUnit, selectedUnit.preMovementPosition);

            RestoreMovementConstraintsAndOverlay();
            cursor.SetPosition(selectedUnit.gridPosition);
            cursor.SetMode(CursorMode.UnitSelected);
        }

        public void DeselectUnit()
        {
            ResetState();
            cursor.ReturnToMemorizedPosition();
        }

        public void ResetState()
        {
            cantoFlow?.ResetState();
            actionMenuFlow?.ClearLastChoice();
            selectedUnit = null;
            validMoveTiles = null;
            rangeHighlighter?.ClearAll();
            pathArrowRenderer?.Clear();
            cursor.SetMode(CursorMode.Free);
        }

        // --- Action / turn dispatch ---

        // Post-action onComplete callback. Hands off to canto re-entry if
        // applicable; otherwise finalizes the unit's turn.
        public void CompleteAction()
        {
            if (cantoFlow.TryEnterCanto(selectedUnit, currentReachability, validMoveTiles)) return;
            FinishTurn();
        }

        public void FinishTurn()
        {
            if (selectedUnit != null)
            {
                if (TurnManager.Instance != null)
                    TurnManager.Instance.UnitRegistry.MarkActed(selectedUnit);
                else
                    selectedUnit.MarkActed();
            }

            memorizedPosition = null;
            ResetState();
            TurnManager.Instance?.CheckAutoEndPlayerPhase();
        }

        // Called by GridCursor.SetPositionWithMemory / ReturnToMemorizedPosition,
        // which stay on GridCursor as the public test surface.
        public void RecordMemorizedPosition(Vector2Int position) => memorizedPosition = position;

        public bool TryConsumeMemorizedPosition(out Vector2Int restored)
        {
            if (!memorizedPosition.HasValue) { restored = default; return false; }
            restored = memorizedPosition.Value;
            memorizedPosition = null;
            return true;
        }

        // --- Internals ---

        private void OnMovementComplete()
        {
            if (cantoFlow.IsCantoMode)
            {
                cantoFlow.FinalizeCanto(selectedUnit);
                FinishTurn();
                return;
            }
            ShowActionMenu();
        }

        public void ShowActionMenu()
        {
            actionMenuFlow.Show(selectedUnit, committedDestination,
                onComplete: CompleteAction,
                onCancelToUnitSelected: RestoreFromActionCancel);
        }

        private void ClearOverlay()
        {
            rangeHighlighter?.ClearAll();
            pathArrowRenderer?.Clear();
        }

        private static bool PathExists(List<Vector2Int> path) =>
            path != null && path.Count > 1;

        // --- Selectability + lookup helpers ---

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == pos)
                    return unit;
            return null;
        }

        private static bool IsUnitSelectable(TestUnit unit)
        {
            if (unit == null) return false;

            if (TurnManager.Instance != null)
            {
                var registry = TurnManager.Instance.UnitRegistry;
                if (!registry.CanAct(unit)) return false;
                if (registry.GetFaction(unit) != Faction.Player) return false;
                if (TurnManager.Instance.CurrentPhase != BattlePhase.PlayerPhase) return false;
                return true;
            }

            // Test seam: no TurnManager means we fall back to the unit's own flag.
            return !unit.hasActed;
        }
    }
}
