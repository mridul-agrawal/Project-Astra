using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Turns the four map facings into unit vectors and back.
    public static class Cardinal
    {
        public static Vector2 ToVector(Facing facing) => facing switch
        {
            Facing.North => Vector2.up,
            Facing.South => Vector2.down,
            Facing.East => Vector2.right,
            Facing.West => Vector2.left,
            _ => Vector2.zero
        };

        public static bool IsHorizontal(Facing facing) => facing == Facing.East || facing == Facing.West;

        // True when the two face along the same axis, whether or not they point the same way.
        public static bool SharesAxis(Facing a, Facing b) => IsHorizontal(a) == IsHorizontal(b);

        public static Facing Opposite(Facing facing) => facing switch
        {
            Facing.North => Facing.South,
            Facing.South => Facing.North,
            Facing.East => Facing.West,
            _ => Facing.East
        };
    }
}
