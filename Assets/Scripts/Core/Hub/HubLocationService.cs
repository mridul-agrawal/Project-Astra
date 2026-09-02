using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Single source of truth for "which room are we standing in and what blocks movement in it".
    // Set once per location load, then read anywhere via HubLocationService.Instance — the same
    // shape as MapService, and the reason nothing else needs a serialized reference to the map.
    public class HubLocationService
    {
        public static HubLocationService Instance { get; private set; }

        private readonly HubLocationData location;
        private readonly HubCollisionMap collision;

        private HubLocationService(HubLocationData location)
        {
            this.location = location;
            collision = location.BuildCollisionMap();
        }

        // The single set-point. HubBootstrapper calls it on visit load, and the location
        // switcher calls it again at every doorway.
        public static void Load(HubLocationData location)
        {
            Instance = location != null ? new HubLocationService(location) : null;
        }

        public HubLocationData CurrentLocation => location;
        public HubCollisionMap Collision => collision;
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
