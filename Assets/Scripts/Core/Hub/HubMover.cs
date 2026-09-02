using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Moves a footprint through the collision map at a constant speed, one axis at a time.
    public static class HubMover
    {
        // 3.5 tiles per second is 112 px/s at the project's 32px tiles.
        public const float DefaultSpeedTilesPerSecond = 3.5f;

        // Never step more than half a collision cell at once, so a thin wall can't be tunnelled
        // through on a frame that hitches.
        private const float MaxSubStep = HubCollisionMap.CellSize * 0.5f;

        // Refines the blocked sub-step so the player stops flush against the wall instead of up to
        // a sub-step short of it, which at this zoom would read as a gap.
        private const int SnugSteps = 8;

        private const float MovedThreshold = 1e-5f;

        // Constant velocity: she starts and stops on the frame the button does, with no acceleration,
        // deceleration or drift.
        public static Vector2 Move(HubCollisionMap map, Vector2 position, Rect footprintOffset,
            Facing? direction, float deltaTime, out bool moved, float speed = DefaultSpeedTilesPerSecond)
        {
            moved = false;
            if (map == null || direction == null || deltaTime <= 0f || speed <= 0f) return position;

            Vector2 delta = Cardinal.ToVector(direction.Value) * (speed * deltaTime);

            // Resolved independently so a blocked axis stops while the other keeps going. Cardinal
            // movement only ever drives one of them, but keeping it general costs nothing and means
            // a scripted diagonal route wouldn't stick on a corner.
            Vector2 result = SweepAxis(map, position, footprintOffset, new Vector2(delta.x, 0f));
            result = SweepAxis(map, result, footprintOffset, new Vector2(0f, delta.y));

            moved = (result - position).sqrMagnitude > MovedThreshold * MovedThreshold;
            return result;
        }

        public static Rect FootprintAt(Vector2 position, Rect footprintOffset) =>
            new(position.x + footprintOffset.x, position.y + footprintOffset.y,
                footprintOffset.width, footprintOffset.height);

        private static Vector2 SweepAxis(HubCollisionMap map, Vector2 position, Rect offset, Vector2 axisDelta)
        {
            float distance = axisDelta.magnitude;
            if (distance <= 0f) return position;

            Vector2 step = axisDelta / distance;
            Vector2 current = position;
            float remaining = distance;

            while (remaining > 0f)
            {
                float length = Mathf.Min(remaining, MaxSubStep);
                Vector2 candidate = current + step * length;

                if (map.IsRectBlocked(FootprintAt(candidate, offset)))
                    return SnugUpTo(map, current, offset, step, length);

                current = candidate;
                remaining -= length;
            }
            return current;
        }

        // Bisects the one sub-step that hit something, returning the furthest position that is
        // still clear.
        private static Vector2 SnugUpTo(HubCollisionMap map, Vector2 from, Rect offset, Vector2 step, float length)
        {
            float safe = 0f;
            float blocked = length;

            for (int i = 0; i < SnugSteps; i++)
            {
                float middle = (safe + blocked) * 0.5f;
                if (map.IsRectBlocked(FootprintAt(from + step * middle, offset))) blocked = middle;
                else safe = middle;
            }
            return from + step * safe;
        }
    }
}
