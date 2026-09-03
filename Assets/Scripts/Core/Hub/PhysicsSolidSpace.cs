using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Answers "can she stand here" from the colliders a designer drew in the room.
    public sealed class PhysicsSolidSpace : ISolidSpace
    {
        // Everything that blocks movement sits on this layer. An interaction volume does not, and is
        // a trigger besides, so a prompt is never something she can walk into.
        public const string SolidLayer = "HubSolid";

        // One is enough: the sweep only ever asks whether anything is there.
        private static readonly Collider2D[] Hit = new Collider2D[1];

        private readonly Rect bounds;
        private readonly ContactFilter2D filter;

        public PhysicsSolidSpace(Rect bounds)
        {
            this.bounds = bounds;

            int mask = LayerMask.GetMask(SolidLayer);
            if (mask == 0)
                Debug.LogError($"[PhysicsSolidSpace] No '{SolidLayer}' layer, so nothing will block her.");

            filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = mask };
        }

        public bool IsBlocked(Rect footprint) =>
            !InsideTheRoom(footprint) ||
            Physics2D.OverlapBox(footprint.center, footprint.size, 0f, filter, Hit) > 0;

        // The room's own edge blocks, so a designer never has to fence the outside of a map.
        private bool InsideTheRoom(Rect footprint) =>
            footprint.xMin >= bounds.xMin && footprint.xMax <= bounds.xMax &&
            footprint.yMin >= bounds.yMin && footprint.yMax <= bounds.yMax;
    }
}
