using System;
using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Works out which of the four directions are worth showing an arrow for. Over an empty
    // map that means "not off the edge"; with a unit selected it means "inside what's left of
    // this unit's movement", recomputed every step.
    //
    // The locked design decision is that the selected state only ever offers arrows the
    // player can actually take, so this is a module every variant can switch on rather than
    // something baked into one of them.
    public static class DirectionalHintModule
    {
        public const int DirectionCount = 4;

        // Order matches the edge slots: N, W, S, E.
        public static void Compute(bool[] into, Vector2Int cursor, Func<Vector2Int, bool> isReachable)
        {
            for (int i = 0; i < DirectionCount; i++)
            {
                Vector2Int step = CursorSlotGeometry.StepOf(CursorSlotGeometry.CornerCount + i);
                into[i] = isReachable == null || isReachable(cursor + step);
            }
        }

        public static void SetAll(bool[] into, bool value)
        {
            for (int i = 0; i < DirectionCount; i++)
                into[i] = value;
        }
    }
}
