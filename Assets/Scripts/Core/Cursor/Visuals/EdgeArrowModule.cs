using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Places four arrows at the edge midpoints — never a closed border, so the tile art and
    // the unit standing on it stay readable. An arrow whose direction is invalid is hidden
    // rather than dimmed, because a dim arrow still reads as an option.
    public static class EdgeArrowModule
    {
        public static void WriteTargets(CursorPose[] targets, in CursorStateVisual visual,
            bool enabled, bool[] validDirections)
        {
            for (int i = 0; i < CursorSlotGeometry.CornerCount; i++)
            {
                int slot = CursorSlotGeometry.CornerCount + i;
                bool show = enabled && (validDirections == null || validDirections[i]);
                targets[slot] = show ? PoseFor(slot, visual) : CursorPose.Hidden;
            }
        }

        public static CursorPose PoseFor(int slot, in CursorStateVisual visual)
        {
            // Pointing inward flips the sprite and tucks it closer, so the arrows converge on
            // the unit instead of fleeing it.
            float rotation = CursorSlotGeometry.RotationOf(slot) + (visual.arrowsPointInward ? 180f : 0f);
            float inset = visual.arrowsPointInward ? visual.inset * 0.8f : visual.inset;

            return new CursorPose
            {
                offset = CursorSlotGeometry.DirectionOf(slot) * inset,
                rotation = rotation,
                scale = visual.pieceScale,
                tint = visual.tint,
                visible = true,
            };
        }
    }
}
