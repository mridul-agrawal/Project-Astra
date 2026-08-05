using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // The eight positions a cursor piece can occupy. Every variant allocates all eight and
    // hides what it doesn't use, so switching variants mid-play never creates or destroys
    // a GameObject.
    public enum CursorSlot
    {
        CornerNE = 0,
        CornerNW = 1,
        CornerSW = 2,
        CornerSE = 3,
        EdgeN = 4,
        EdgeW = 5,
        EdgeS = 6,
        EdgeE = 7
    }

    public static class CursorSlotGeometry
    {
        public const int SlotCount = 8;
        public const int CornerCount = 4;

        // Corners first, then edges, both counter-clockwise from the top — so a morph from
        // slot i's corner to slot i's edge is always a quarter turn in a consistent direction.
        private static readonly Vector2[] Directions =
        {
            new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f),
            new(0f, 1f), new(-1f, 0f), new(0f, -1f), new(1f, 0f),
        };

        // Sprites are authored pointing up; this is the spin that aims each one outward.
        private static readonly float[] Rotations = { -45f, 45f, 135f, 225f, 0f, 90f, 180f, 270f };

        public static bool IsCorner(int slot) => slot < CornerCount;

        // Normalised so a corner piece and an edge piece sit the same distance from the
        // centre for a given inset, instead of the corner flying out to 1.41x.
        public static Vector2 DirectionOf(int slot) => Directions[slot].normalized;

        public static float RotationOf(int slot) => Rotations[slot];

        // The edge slot a corner piece morphs into. Pairing i with i+4 keeps every piece's
        // sweep a quarter turn in the same rotational direction.
        public static int EdgeSlotForCorner(int corner) => corner + CornerCount;

        public static Vector2Int StepOf(int edgeSlot) => edgeSlot switch
        {
            (int)CursorSlot.EdgeN => Vector2Int.up,
            (int)CursorSlot.EdgeW => Vector2Int.left,
            (int)CursorSlot.EdgeS => Vector2Int.down,
            _ => Vector2Int.right,
        };
    }
}
