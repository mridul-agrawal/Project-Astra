using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // Turns a unit's frame-to-frame world movement into a run/idle decision plus
    // a facing. Kept stateful (not a static call) so it can remember the last
    // facing and hold "running" briefly after motion stops — that short hold is
    // what stops a multi-tile walk from flickering to idle between tiles.
    public sealed class FacingClassifier
    {
        private readonly float movingSpeedThreshold;   // tiles/sec below which we read "still"
        private readonly float idleHoldSeconds;        // stay running this long after motion stops
        private readonly float teleportTiles;          // a jump bigger than this is a snap, not a walk

        private Facing facing = Facing.South;
        private float timeSinceMoving;
        private bool moving;

        public FacingClassifier(
            float movingSpeedThreshold = 0.5f,
            float idleHoldSeconds = 0.05f,
            float teleportTiles = 1.5f)
        {
            this.movingSpeedThreshold = movingSpeedThreshold;
            this.idleHoldSeconds = idleHoldSeconds;
            this.teleportTiles = teleportTiles;
            timeSinceMoving = idleHoldSeconds;
        }

        public Facing Facing => facing;
        public bool Moving => moving;

        // Feed one frame of the root transform's movement. delta is in world
        // units (which equal tiles here); dt is that frame's delta time.
        public void Tick(Vector2 delta, float dt)
        {
            if (dt <= 0f) return;

            float distance = delta.magnitude;
            if (distance > teleportTiles) { SnapToIdle(); return; }

            float speed = distance / dt;
            if (speed >= movingSpeedThreshold)
                StartMoving(delta);
            else
                CoastTowardIdle(dt);
        }

        private void StartMoving(Vector2 delta)
        {
            facing = DominantDirection(delta);
            moving = true;
            timeSinceMoving = 0f;
        }

        private void CoastTowardIdle(float dt)
        {
            timeSinceMoving += dt;
            if (timeSinceMoving >= idleHoldSeconds) moving = false;
        }

        private void SnapToIdle()
        {
            moving = false;
            timeSinceMoving = idleHoldSeconds;
        }

        private static Facing DominantDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0f ? Facing.East : Facing.West;
            return delta.y > 0f ? Facing.North : Facing.South;
        }
    }
}
