using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul.Events
{
    // Works out which way a character should step to reach the next corner of an authored route.
    //
    // Horizontal first, then vertical, so a leg is always one cardinal direction — the same rule the
    // player moves under, and the reason a scripted walk never produces a diagonal.
    //
    // Arrival has a tolerance and the caller snaps to the corner on reaching it, so a character
    // settles instead of shuffling back and forth over the last fraction of a pixel.
    public static class CardinalRouteFollower
    {
        // Under a third of a pixel at 32px tiles: close enough to snap, far enough not to trip early.
        public const float ArrivalTolerance = 0.01f;

        public static bool HasArrived(Vector2 from, Vector2 to) =>
            Mathf.Abs(to.x - from.x) <= ArrivalTolerance && Mathf.Abs(to.y - from.y) <= ArrivalTolerance;

        // Null once the corner is reached.
        public static Facing? NextStep(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            if (Mathf.Abs(dx) > ArrivalTolerance) return dx > 0f ? Facing.East : Facing.West;

            float dy = to.y - from.y;
            if (Mathf.Abs(dy) > ArrivalTolerance) return dy > 0f ? Facing.North : Facing.South;

            return null;
        }

        // Stops the step overshooting the corner, which at a low frame rate would otherwise send the
        // character past it and straight into walking back.
        public static Vector2 ClampToCorner(Vector2 from, Vector2 stepped, Vector2 corner, Facing direction)
        {
            bool horizontal = Cardinal.IsHorizontal(direction);
            float travelled = horizontal ? stepped.x - from.x : stepped.y - from.y;
            float remaining = horizontal ? corner.x - from.x : corner.y - from.y;

            if (Mathf.Abs(travelled) < Mathf.Abs(remaining)) return stepped;
            return horizontal ? new Vector2(corner.x, from.y) : new Vector2(from.x, corner.y);
        }
    }
}
