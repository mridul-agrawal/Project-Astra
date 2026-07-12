using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Canto: cavalry and flying classes get a second move-only round after
    // any non-Wait action. CantoFlow owns the "should canto fire?" decision,
    // the pre-canto movement-point bookkeeping, and the FinalizeCanto unwind
    // that restores the unit's full movement budget on completion.
    //
    // Lifecycle: GridCursor.CompleteAction calls TryEnterCanto. If it
    // returns true, canto is active and the unit is back in UnitSelected
    // mode. The next action's OnMovementComplete (or a Cancel) calls
    // FinalizeCanto, which restores movement points and invokes the
    // caller's turn-finalization callback.
    public class CantoFlow
    {
        private readonly GridCursor cursor;
        private readonly ActionMenuFlow actionMenuFlow;

        private bool isCantoMode;
        private int preCantoMovementPoints;

        public bool IsCantoMode => isCantoMode;

        public CantoFlow(GridCursor cursor, ActionMenuFlow actionMenuFlow)
        {
            this.cursor = cursor;
            this.actionMenuFlow = actionMenuFlow;
        }

        // Returns true and re-enters UnitSelected if canto applies; false
        // otherwise (caller should proceed to turn finalization). reachability
        // is the result computed for the unit's original movement budget —
        // we use its CostMap to derive how much movement the unit has left.
        public bool TryEnterCanto(
            TestUnit selectedUnit,
            Pathfinder.ReachabilityResult reachability,
            HashSet<Vector2Int> validMoveTiles)
        {
            if (isCantoMode) return false;
            if (selectedUnit == null) return false;

            var lastChoice = actionMenuFlow?.LastChoice;
            if (lastChoice == ActionChoice.Wait || lastChoice == null) return false;

            var cls = selectedUnit.UnitInstance?.CurrentClass;
            if (cls == null || !cls.HasCanto) return false;

            int costPaid = reachability.CostMap != null
                && reachability.CostMap.TryGetValue(selectedUnit.gridPosition, out var c) ? c : 0;
            int remaining = selectedUnit.movementPoints - costPaid;
            if (remaining <= 0) return false;

            preCantoMovementPoints = selectedUnit.movementPoints;
            selectedUnit.movementPoints = remaining;
            isCantoMode = true;

            cursor.EnterUnitSelectedMode();

            // The unit's own tile is a legal "stay put" exit during canto, so
            // make sure the constraint set includes it even when reachability
            // would otherwise exclude it (e.g. zero-cost moves).
            cursor.EnsureMoveTileAllowed(selectedUnit.gridPosition);

            return true;
        }

        // Restores the unit's full movement budget and clears canto state.
        // Caller still owns running turn-end cleanup (mark-acted + reset);
        // CantoFlow's contract ends at canto unwind.
        public void FinalizeCanto(TestUnit selectedUnit)
        {
            isCantoMode = false;
            if (selectedUnit != null)
                selectedUnit.movementPoints = preCantoMovementPoints;
            actionMenuFlow?.ClearLastChoice();
        }

        public void ResetState() => isCantoMode = false;
    }
}
