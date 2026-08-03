using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Environment
{
    // A smooth curve that passes through a list of points. Feed it control points
    // and a t from 0 (first point) to 1 (last point); it returns a position that
    // eases through every point — a natural arc rather than straight legs. Pure
    // math, no Unity lifecycle, so it can be unit-tested.
    public static class CatmullRom
    {
        public static Vector2 Evaluate(IReadOnlyList<Vector2> points, float t)
        {
            if (points == null || points.Count == 0) return Vector2.zero;
            if (points.Count == 1) return points[0];

            int segments = points.Count - 1;
            float scaled = Mathf.Clamp01(t) * segments;
            int i = Mathf.Min((int)scaled, segments - 1);
            float local = scaled - i;

            Vector2 p0 = points[Mathf.Max(i - 1, 0)];
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            return Spline(p0, p1, p2, p3, local);
        }

        private static Vector2 Spline(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (p2 - p0) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
