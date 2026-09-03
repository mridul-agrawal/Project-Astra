using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Single source of truth for which room she is in and what blocks movement in it.
    public class HubLocationService
    {
        public static HubLocationService Instance { get; private set; }

        private readonly HubLocationData location;
        private readonly HubCollisionMap collision;

        private HubLocationService(HubLocationData location)
        {
            this.location = location;
            collision = location.BuildCollisionMap();
            Solids = new PhysicsSolidSpace(location.Bounds);
        }

        // The single set-point. HubBootstrapper calls it on visit load, and the location
        // switcher calls it again at every doorway.
        public static void Load(HubLocationData location)
        {
            Instance = location != null ? new HubLocationService(location) : null;
        }

        public HubLocationData CurrentLocation => location;
        public HubCollisionMap Collision => collision;

        // What movement asks: the colliders a designer drew, inside the room's own edges.
        public ISolidSpace Solids { get; }
        public Rect Bounds => location != null ? location.Bounds : Rect.zero;

        // Props that change state mid-visit stamp and un-stamp through here, so the collision map
        // and what the player can see never disagree.
        public void SetPropSolid(string propId, bool solid)
        {
            foreach (HubPropFootprint prop in location.PropFootprints)
            {
                if (prop.propId != propId) continue;
                if (solid) collision.Stamp(prop.footprint);
                else collision.Unstamp(prop.footprint);
                return;
            }
            Debug.LogWarning($"[HubLocationService] No prop footprint named '{propId}' in {location.LocationId}.");
        }
    }
}
