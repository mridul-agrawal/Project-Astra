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
        private readonly PathfindingService _pathfindingService;
        private readonly UnitMover _unitMover;
        private readonly RangeHighlighter _rangeHighlighter;
        private readonly PathArrowRenderer _pathArrowRenderer;
        private readonly CombatForecastUI _combatForecastUI;
        private readonly GridCursor _cursor;
        private readonly CantoFlow _cantoFlow;
        private readonly ActionMenuFlow _actionMenuFlow;

        private TestUnit _selectedUnit;
        private Pathfinder.ReachabilityResult _currentReachability;
        private Vector2Int _committedDestination;
        private HashSet<Vector2Int> _validMoveTiles;
        private Vector2Int? _memorizedPosition;

        // --- Read-only accessors (consumed by GridCursor + other flows + tests) ---

        public TestUnit SelectedUnit => _selectedUnit;
        public Vector2Int CommittedDestination => _committedDestination;
        public Pathfinder.ReachabilityResult CurrentReachability => _currentReachability;
        public HashSet<Vector2Int> ValidMoveTiles => _validMoveTiles;
        public bool IsMovementConstrained => _validMoveTiles != null;

        public UnitSelectionFlow(
            PathfindingService pathfindingService,
            UnitMover unitMover,
            RangeHighlighter rangeHighlighter,
            PathArrowRenderer pathArrowRenderer,
            CombatForecastUI combatForecastUI,
            GridCursor cursor,
            CantoFlow cantoFlow,
            ActionMenuFlow actionMenuFlow)
        {
            _pathfindingService = pathfindingService;
            _unitMover = unitMover;
            _rangeHighlighter = rangeHighlighter;
            _pathArrowRenderer = pathArrowRenderer;
            _combatForecastUI = combatForecastUI;
            _cursor = cursor;
            _cantoFlow = cantoFlow;
            _actionMenuFlow = actionMenuFlow;
        }

        // --- Entry points (HandleConfirm dispatches here) ---

        public void TrySelectUnit(Vector2Int cursorPos)
        {
            TestUnit unit = FindUnitAt(cursorPos);
            if (!IsUnitSelectable(unit)) return;

            _selectedUnit = unit;
            EnterUnitSelectedMode();
        }

        public void TryCommitMovement(Vector2Int destination)
        {
            if (!_currentReachability.Destinations.Contains(destination)) return;

            _committedDestination = destination;
            _selectedUnit.preMovementPosition = _selectedUnit.gridPosition;

            var path = Pathfinder.ReconstructPath(_selectedUnit.gridPosition, _committedDestination, _currentReachability);

            ClearOverlay();
            _cursor.SetMode(CursorMode.Locked);

            if (PathExists(path))
                _unitMover.MoveAlongPath(_selectedUnit, path, OnMovementComplete);
            else
                OnMovementComplete();
        }

        // --- Cursor-driven (HandleCursorMove on UnitSelected) ---

        public void UpdatePathArrow(Vector2Int cursorPos)
        {
            if (_pathArrowRenderer == null || _selectedUnit == null) return;

            if (!_currentReachability.Destinations.Contains(cursorPos))
            {
                _pathArrowRenderer.Clear();
                return;
            }

            var path = Pathfinder.ReconstructPath(_selectedUnit.gridPosition, cursorPos, _currentReachability);
            _pathArrowRenderer.ShowPath(path);
        }

        // --- Mutation seams used by ActionMenuFlow + CantoFlow ---

        public void SetValidMoveTiles(HashSet<Vector2Int> tiles) => _validMoveTiles = tiles;

        // CantoFlow: ensures the unit's current tile counts as a legal
        // "stay put" exit even when reachability would exclude it.
        public void EnsureMoveTileAllowed(Vector2Int tile)
        {
            if (_validMoveTiles != null && !_validMoveTiles.Contains(tile))
                _validMoveTiles.Add(tile);
        }

        // Called by CantoFlow at canto entry and by TrySelectUnit on initial
        // selection. Recomputes reachability against the unit's CURRENT
        // movement points (which canto has just reduced).
        public void EnterUnitSelectedMode()
        {
            if (_pathfindingService == null || _selectedUnit == null) return;

            _currentReachability = _pathfindingService.ComputeReachability(
                _selectedUnit.gridPosition, _selectedUnit.movementPoints,
                _selectedUnit.movementType, GetOccupantType);

            RestoreMovementConstraintsAndOverlay();
            _memorizedPosition = _cursor.GridPosition;
            _cursor.SetPosition(_selectedUnit.gridPosition);
            _cursor.SetMode(CursorMode.UnitSelected);
        }

        // Hook for the unit-occupancy service. Pathfinder treats every tile
        // as unoccupied until the unit-management system wires this up.
        // TODO(refactor): swap for a real occupancy lookup when it lands.
        private Pathfinder.OccupantType GetOccupantType(Vector2Int pos) => Pathfinder.OccupantType.None;

        public void RestoreMovementConstraintsAndOverlay()
        {
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);
            _rangeHighlighter?.ShowMovementRange(_currentReachability.Destinations, _currentReachability.PassThrough);
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
            _cantoFlow.FinalizeCanto(_selectedUnit);
            FinishTurn();
        }

        public void RestoreFromActionCancel()
        {
            _combatForecastUI?.Hide();

            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            RestoreMovementConstraintsAndOverlay();
            _cursor.SetPosition(_selectedUnit.gridPosition);
            _cursor.SetMode(CursorMode.UnitSelected);
        }

        public void DeselectUnit()
        {
            ResetState();
            _cursor.ReturnToMemorizedPosition();
        }

        public void ResetState()
        {
            _cantoFlow?.ResetState();
            _actionMenuFlow?.ClearLastChoice();
            _selectedUnit = null;
            _validMoveTiles = null;
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            _cursor.SetMode(CursorMode.Free);
        }

        // --- Action / turn dispatch ---

        // Post-action onComplete callback. Hands off to canto re-entry if
        // applicable; otherwise finalizes the unit's turn.
        public void CompleteAction()
        {
            if (_cantoFlow.TryEnterCanto(_selectedUnit, _currentReachability, _validMoveTiles)) return;
            FinishTurn();
        }

        public void FinishTurn()
        {
            if (_selectedUnit != null)
            {
                if (TurnManager.Instance != null)
                    TurnManager.Instance.UnitRegistry.MarkActed(_selectedUnit);
                else
                    _selectedUnit.MarkActed();
            }

            _memorizedPosition = null;
            ResetState();
            TurnManager.Instance?.CheckAutoEndPlayerPhase();
        }

        // Called by GridCursor.SetPositionWithMemory / ReturnToMemorizedPosition,
        // which stay on GridCursor as the public test surface.
        public void RecordMemorizedPosition(Vector2Int position) => _memorizedPosition = position;

        public bool TryConsumeMemorizedPosition(out Vector2Int restored)
        {
            if (!_memorizedPosition.HasValue) { restored = default; return false; }
            restored = _memorizedPosition.Value;
            _memorizedPosition = null;
            return true;
        }

        // --- Internals ---

        private void OnMovementComplete()
        {
            if (_cantoFlow.IsCantoMode)
            {
                _cantoFlow.FinalizeCanto(_selectedUnit);
                FinishTurn();
                return;
            }
            ShowActionMenu();
        }

        public void ShowActionMenu()
        {
            _actionMenuFlow.Show(_selectedUnit, _committedDestination,
                onComplete: CompleteAction,
                onCancelToUnitSelected: RestoreFromActionCancel);
        }

        private void ClearOverlay()
        {
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
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
