using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Single source of truth for "which room are we standing in and what blocks movement in it".
    // Set once per location load, then read anywhere via GurukulLocationService.Instance — the same
    // shape as MapService, and the reason nothing else needs a serialized reference to the map.
    public class GurukulLocationService
    {
        public static GurukulLocationService Instance { get; private set; }

        private readonly GurukulLocationData location;
        private readonly GurukulCollisionMap collision;

        private GurukulLocationService(GurukulLocationData location)
        {
            this.location = location;
            collision = location.BuildCollisionMap();
        }

        // The single set-point. GurukulBootstrapper calls it on visit load, and the location
        // switcher calls it again at every doorway.
        public static void Load(GurukulLocationData location)
        {
            Instance = location != null ? new GurukulLocationService(location) : null;
        }

        public GurukulLocationData CurrentLocation => location;
        public GurukulCollisionMap Collision => collision;
        public Rect Bounds => location != null ? location.Bounds : Rect.zero;

        // Props that change state mid-visit stamp and un-stamp through here, so the collision map
        // and what the player can see never disagree.
        public void SetPropSolid(string propId, bool solid)
        {
            foreach (GurukulPropFootprint prop in location.PropFootprints)
            {
                if (prop.propId != propId) continue;
                if (solid) collision.Stamp(prop.footprint);
                else collision.Unstamp(prop.footprint);
                return;
            }
            Debug.LogWarning($"[GurukulLocationService] No prop footprint named '{propId}' in {location.LocationId}.");
        }
    }
}
