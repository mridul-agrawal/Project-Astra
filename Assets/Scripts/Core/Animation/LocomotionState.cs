using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // The animator parameters that describe what a unit should be doing this
    // frame.
    public readonly struct LocomotionParams
    {
        public readonly float MoveX;
        public readonly float MoveY;
        public readonly bool Moving;
        public readonly bool Selected;

        public LocomotionParams(float moveX, float moveY, bool moving, bool selected)
        {
            MoveX = moveX;
            MoveY = moveY;
            Moving = moving;
            Selected = selected;
        }
    }

    // Decides a unit's animation state from its raw inputs. Priority is Moving
    // over Selected over Idle — encoded here, not buried in the animator graph,
    // so it can be unit-tested.
    public static class LocomotionState
    {
        public static LocomotionParams Select(bool moving, Facing facing, bool selected)
        {
            if (moving)
            {
                Vector2 dir = DirectionVector(facing);
                return new LocomotionParams(dir.x, dir.y, true, false);
            }
            return new LocomotionParams(0f, 0f, false, selected);
        }

        private static Vector2 DirectionVector(Facing facing) => facing switch
        {
            Facing.North => Vector2.up,
            Facing.South => Vector2.down,
            Facing.East => Vector2.right,
            Facing.West => Vector2.left,
            _ => Vector2.zero
        };
    }
}
