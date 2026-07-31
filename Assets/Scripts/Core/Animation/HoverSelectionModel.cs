using System;
using UnityEngine;
using ProjectAstra.Core.Units;
using CursorMode = ProjectAstra.Core.Cursor.CursorMode;

namespace ProjectAstra.Core.Animation
{
    // Decides which unit should show its "selected" pose. While the player is
    // just browsing, that's whatever selectable unit sits under the cursor. Once
    // a unit is picked up, it stays selected through its whole action no matter
    // where the cursor wanders.
    public static class HoverSelectionModel
    {
        public static TestUnit ResolveSelected(
            CursorMode mode,
            Vector2Int cursorTile,
            TestUnit pickedUpUnit,
            Func<Vector2Int, TestUnit> unitAt,
            Func<TestUnit, bool> isSelectable)
        {
            if (IsCommittedToUnit(mode)) return pickedUpUnit;
            if (mode != CursorMode.Free) return null;

            TestUnit hovered = unitAt?.Invoke(cursorTile);
            if (hovered == null) return null;
            return isSelectable != null && isSelectable(hovered) ? hovered : null;
        }

        private static bool IsCommittedToUnit(CursorMode mode) =>
            mode == CursorMode.UnitSelected ||
            mode == CursorMode.ActionMenu ||
            mode == CursorMode.Targeting;
    }
}
