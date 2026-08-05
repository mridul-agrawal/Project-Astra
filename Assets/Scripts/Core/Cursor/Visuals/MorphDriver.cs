namespace ProjectAstra.Core.Cursor
{
    // The Morphing Compass behaviour: one set of four pieces that lives at the corners over
    // an empty tile and reshapes into edge arrows over a unit.
    //
    // It only decides the target poses — the sweep itself is the piece's own morph, which is
    // why a retarget halfway through is free. Pairing corner i with edge i+4 means all four
    // pieces travel a quarter turn the same way round, so the set reads as one object
    // rotating rather than four things scattering.
    public static class MorphDriver
    {
        public static void WriteTargets(CursorPose[] targets, in CursorStateVisual visual,
            bool morphToEdges, bool[] validDirections)
        {
            for (int corner = 0; corner < CursorSlotGeometry.CornerCount; corner++)
            {
                targets[corner] = morphToEdges
                    ? EdgePoseFor(corner, visual, validDirections)
                    : CornerBracketModule.PoseFor(corner, visual);
            }

            // The dedicated edge slots stay out of the way; a morph variant only ever uses four pieces.
            for (int slot = CursorSlotGeometry.CornerCount; slot < CursorSlotGeometry.SlotCount; slot++)
                targets[slot] = CursorPose.Hidden;
        }

        private static CursorPose EdgePoseFor(int corner, in CursorStateVisual visual, bool[] validDirections)
        {
            int edgeSlot = CursorSlotGeometry.EdgeSlotForCorner(corner);
            int direction = edgeSlot - CursorSlotGeometry.CornerCount;

            if (validDirections != null && !validDirections[direction])
                return CursorPose.Hidden;

            return EdgeArrowModule.PoseFor(edgeSlot, visual);
        }

        // Which sprite a morphing piece should be wearing right now. Swapped at the halfway
        // point so the change lands while the piece is mid-sweep and least conspicuous.
        public static bool ShouldShowArrowSprite(bool morphToEdges) => morphToEdges;
    }
}
