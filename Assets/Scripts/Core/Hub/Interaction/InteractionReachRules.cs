using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // The two questions most interactables ask about the player: is she facing it, can she see it.
    public static class InteractionReachRules
    {
        // The spec's "within one environmental tile". The size a trigger is built to when nobody
        // authored one by hand.
        public const float DefaultReachTiles = 1.0f;

        // A bit over half a tile, so standing slightly off to one side still counts as facing it.
        public const float DefaultLateralTolerance = 0.6f;

        private static readonly RaycastHit2D[] Hit = new RaycastHit2D[1];
        private static ContactFilter2D? solidsOnly;

        // Built with useTriggers off, because a linecast otherwise obeys the project-wide
        // queriesHitTriggers setting and an interaction volume would block the view of its own target.
        private static ContactFilter2D SolidsOnly =>
            solidsOnly ??= new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = LayerMask.GetMask(PhysicsSolidSpace.SolidLayer)
            };

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

        // Walls, counters and the river all break an interaction; the target's own body does not,
        // which is what the margin stops the cast reaching.
        public static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 offset = to - from;
            float distance = offset.magnitude - TargetBodyMargin;
            if (distance <= 0f) return true;

            Vector2 stopShortOf = from + offset.normalized * distance;
            return Physics2D.Linecast(from, stopShortOf, SolidsOnly, Hit) == 0;
        }

        // How far off dead-ahead a target sits, for picking between two things in range.
        public static float LateralOffset(InteractorPose player, Vector2 target)
        {
            Vector2 ahead = Cardinal.ToVector(player.Facing);
            Vector2 side = new(-ahead.y, ahead.x);
            return Mathf.Abs(Vector2.Dot(target - player.Position, side));
        }

    }
}
