using UnityEngine;

namespace ProjectAstra.Core.Camera
{
    // Keeps a camera from showing past the edges of the area it is in.
    //
    // Pure arithmetic so the awkward case has a test: an area narrower than the view cannot be
    // clamped, because the lowest allowed centre would sit above the highest. It is centred
    // instead, which is what a room smaller than one screenful should look like.
    public static class CameraContainment
    {
        // The point the camera should actually sit on, given where it wants to be.
        public static Vector2 Contain(Vector2 desired, Rect area, Vector2 viewSize)
        {
            return new Vector2(
                ContainAxis(desired.x, area.xMin, area.xMax, viewSize.x),
                ContainAxis(desired.y, area.yMin, area.yMax, viewSize.y));
        }

        public static float ContainAxis(float desired, float min, float max, float viewSize)
        {
            if (max - min <= viewSize) return (min + max) * 0.5f;

            float half = viewSize * 0.5f;
            return Mathf.Clamp(desired, min + half, max - half);
        }

        // Whole pixels only. A camera resting on a fraction while sprites snap to the grid makes
        // different sprites round different ways, and the whole scene crawls.
        public static Vector2 RoundToPixel(Vector2 position, float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f) return position;

            return new Vector2(
                Mathf.Round(position.x * pixelsPerUnit) / pixelsPerUnit,
                Mathf.Round(position.y * pixelsPerUnit) / pixelsPerUnit);
        }
    }
}
