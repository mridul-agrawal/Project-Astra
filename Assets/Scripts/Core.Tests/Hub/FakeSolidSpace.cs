using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // Blocking drawn by hand, so the sweep and the route search can be tested without a scene.
    public sealed class FakeSolidSpace : ISolidSpace
    {
        private readonly List<Rect> solids = new();
        private readonly Rect bounds;

        public FakeSolidSpace(float width, float height)
        {
            bounds = new Rect(0f, 0f, width, height);
        }

        public FakeSolidSpace Block(Rect area)
        {
            solids.Add(area);
            return this;
        }

        // Blocks the whole rect from (x, y) to (x + w, y + h) in tiles.
        public FakeSolidSpace Block(float x, float y, float w, float h) => Block(new Rect(x, y, w, h));

        public bool IsBlocked(Rect footprint)
        {
            if (!Inside(footprint)) return true;

            foreach (Rect solid in solids)
                if (solid.Overlaps(footprint)) return true;
            return false;
        }

        private bool Inside(Rect footprint) =>
            footprint.xMin >= bounds.xMin && footprint.xMax <= bounds.xMax &&
            footprint.yMin >= bounds.yMin && footprint.yMax <= bounds.yMax;
    }
}
