using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Where one cursor piece sits relative to the tile centre, and how it looks there.
    // Offsets are in world units, and one tile is one world unit, so 0.03125 is the
    // 1-pixel equivalent at the project's 32 pixels-per-unit.
    public struct CursorPose
    {
        public Vector2 offset;
        public float rotation;
        public float scale;
        public Color tint;
        public bool visible;

        public static CursorPose Hidden => new()
        {
            offset = Vector2.zero,
            rotation = 0f,
            scale = 1f,
            tint = Color.clear,
            visible = false,
        };

        // Straight-line blend. Used for everything except a morph that has been given a
        // rotation direction, which sweeps instead — see PolarLerp.
        public static CursorPose Lerp(in CursorPose a, in CursorPose b, float t) => new()
        {
            offset = Vector2.Lerp(a.offset, b.offset, t),
            rotation = Mathf.LerpAngle(a.rotation, b.rotation, t),
            scale = Mathf.Lerp(a.scale, b.scale, t),
            tint = Color.Lerp(a.tint, b.tint, t),
            visible = t < 1f ? a.visible || b.visible : b.visible,
        };

        // Sweeps the offset around the tile centre instead of cutting across it, so the
        // Morphing Compass reads as pieces travelling along the tile edge. Direction forces
        // the angular delta one way (+1 counter-clockwise, -1 clockwise) so all four pieces
        // rotate together rather than each taking its own shortest path.
        public static CursorPose PolarLerp(in CursorPose a, in CursorPose b, float t, int direction)
        {
            float angleA = Mathf.Atan2(a.offset.y, a.offset.x) * Mathf.Rad2Deg;
            float angleB = Mathf.Atan2(b.offset.y, b.offset.x) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(angleA, angleB);

            if (direction > 0 && delta < 0f) delta += 360f;
            if (direction < 0 && delta > 0f) delta -= 360f;

            float angle = (angleA + delta * t) * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(a.offset.magnitude, b.offset.magnitude, t);

            var blended = Lerp(a, b, t);
            blended.offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return blended;
        }
    }
}
