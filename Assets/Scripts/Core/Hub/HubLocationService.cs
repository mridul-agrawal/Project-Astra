using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Single source of truth for which room she is in and what blocks movement in it.
    public class HubLocationService
    {
        public static HubLocationService Instance { get; private set; }

        private readonly HubLocationData location;

        private HubLocationService(HubLocationData location)
        {
            this.location = location;
            Solids = new PhysicsSolidSpace(location.Bounds);
        }

        // The single set-point. HubBootstrapper calls it on visit load, and the location
        // switcher calls it again at every doorway.
        public static void Load(HubLocationData location)
        {
            Instance = location != null ? new HubLocationService(location) : null;
        }

        public HubLocationData CurrentLocation => location;
        // What movement asks: the colliders a designer drew, inside the room's own edges.
        public ISolidSpace Solids { get; }
        public Rect Bounds => location != null ? location.Bounds : Rect.zero;

    }
}
