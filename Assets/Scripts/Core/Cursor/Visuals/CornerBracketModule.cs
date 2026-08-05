using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Places four open brackets at the tile corners. The state's inset is what does the
    // talking: a smaller inset pulls the brackets in toward the unit, which is how
    // "selectable" and "selected" read as tightening rather than just recolouring.
    public static class CornerBracketModule
    {
        public static void WriteTargets(CursorPose[] targets, in CursorStateVisual visual, bool enabled)
        {
            for (int slot = 0; slot < CursorSlotGeometry.CornerCount; slot++)
            {
                targets[slot] = enabled
                    ? PoseFor(slot, visual)
                    : CursorPose.Hidden;
            }
        }

        public static CursorPose PoseFor(int slot, in CursorStateVisual visual) => new()
        {
            offset = CursorSlotGeometry.DirectionOf(slot) * visual.inset,
            rotation = CursorSlotGeometry.RotationOf(slot),
            scale = visual.pieceScale,
            tint = visual.tint,
            visible = true,
        };
    }
}
