using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // The two questions most interactables ask about the player, kept pure so the awkward angles
    // have tests. Offered, not imposed — a signboard readable from any side ignores IsFacing, and
    // anything with its own idea of reach overrides CanReach instead of calling these.
    public static class InteractionReachRules
    {
        // A bit over half a tile, so standing slightly off to one side still counts as facing it.
        public const float DefaultLateralTolerance = 0.6f;

        private const float LineOfSightStep = 0.25f;

        // Stops the sight check short of the target, because a character's own body is solid and
        // would otherwise block the view of itself.
        private const float TargetBodyMargin = 0.4f;

        // Ahead of her and not far off to one side. Measured in her facing's frame rather than as
        // an angle, so the tolerance is a distance a designer can picture.
        public static bool IsFacing(InteractorPose player, Vector2 target,
            float lateralTolerance = DefaultLateralTolerance)
        {
            Vector2 offset = target - player.Position;
            Vector2 ahead = Cardinal.ToVector(player.Facing);
            Vector2 side = new(-ahead.y, ahead.x);

            float forward = Vector2.Dot(offset, ahead);
            float lateral = Mathf.Abs(Vector2.Dot(offset, side));

            return forward > 0f && lateral <= lateralTolerance;
        }

        // Walls, counters and the river all break an interaction; the target's own body does not.
        public static bool HasLineOfSight(Vector2 from, Vector2 to, HubCollisionMap collision)
        {
            if (collision == null) return true;

            Vector2 offset = to - from;
            float distance = offset.magnitude - TargetBodyMargin;
            if (distance <= 0f) return true;

            Vector2 direction = offset.normalized;
            for (float travelled = LineOfSightStep; travelled <= distance; travelled += LineOfSightStep)
            {
                Vector2 point = from + direction * travelled;
                if (collision.IsCellBlocked(CellOf(point.x), CellOf(point.y))) return false;
            }
            return true;
        }

        // How far off dead-ahead a target sits, for picking between two things in range.
        public static float LateralOffset(InteractorPose player, Vector2 target)
        {
            Vector2 ahead = Cardinal.ToVector(player.Facing);
            Vector2 side = new(-ahead.y, ahead.x);
            return Mathf.Abs(Vector2.Dot(target - player.Position, side));
        }

        private static int CellOf(float world) => Mathf.FloorToInt(world / HubCollisionMap.CellSize);
    }
}
