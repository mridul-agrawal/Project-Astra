using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Marks a root object in the Hub scene as one of the game's places.
    //
    // The room is authored here, in the scene, where a designer can see and edit it. This component
    // is only how the game knows which place it is looking at.
    public sealed class HubRoom : MonoBehaviour
    {
        [Tooltip("Which place this is. Create one under Project Astra > Hub if it does not exist yet.")]
        [HubRef, SerializeField] private HubLocationData location;

        public HubLocationData Location => location;
        public string LocationId => location != null ? location.LocationId : null;

        // Editor tooling needs to point a room at a place without going through the inspector.
        public void Bind(HubLocationData place) => location = place;
    }
}
