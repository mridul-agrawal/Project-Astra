using System;
using UnityEngine;
using ProjectAstra.Core.Units;
using CursorMode = ProjectAstra.Core.Cursor.CursorMode;

namespace ProjectAstra.Core.Animation
{
    // Keeps exactly one unit showing its "selected" pose as the cursor moves.
    // Owned by GridCursor, which calls Refresh on cursor moves and mode changes.
    // The actual "who should be lit?" decision lives in HoverSelectionModel (a
    // pure, tested function); this driver just diffs the answer against the last
    // one and flips the flag on the unit that changed.
    public sealed class HoverSelectionDriver
    {
        private readonly Func<CursorMode> currentMode;
        private readonly Func<Vector2Int> cursorTile;
        private readonly Func<TestUnit> pickedUpUnit;
        private readonly Func<Vector2Int, TestUnit> unitAt;
        private readonly Func<TestUnit, bool> isSelectable;

        private TestUnit currentlySelected;

        public HoverSelectionDriver(
            Func<CursorMode> currentMode,
            Func<Vector2Int> cursorTile,
            Func<TestUnit> pickedUpUnit,
            Func<Vector2Int, TestUnit> unitAt,
            Func<TestUnit, bool> isSelectable)
        {
            this.currentMode = currentMode;
            this.cursorTile = cursorTile;
            this.pickedUpUnit = pickedUpUnit;
            this.unitAt = unitAt;
            this.isSelectable = isSelectable;
        }

        public void Refresh()
        {
            TestUnit next = HoverSelectionModel.ResolveSelected(
                currentMode(), cursorTile(), pickedUpUnit(), unitAt, isSelectable);
            if (next == currentlySelected) return;

            SetSelected(currentlySelected, false);
            SetSelected(next, true);
            currentlySelected = next;
        }

        private static void SetSelected(TestUnit unit, bool selected)
        {
            if (unit == null) return;
            unit.GetComponentInChildren<UnitAnimator>()?.SetSelected(selected);
        }
    }
}
