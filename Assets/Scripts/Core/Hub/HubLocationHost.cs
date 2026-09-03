using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Decides which of the scene's rooms is the one she is standing in.
    public sealed class HubLocationHost : MonoBehaviour
    {
        public HubLocationData Current { get; private set; }
        public HubRoom ActiveRoom { get; private set; }

        // Every room is authored in the scene and sits there all the time; only one is ever awake.
        public static HubRoom[] AllRooms() =>
            FindObjectsByType<HubRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        public void Show(HubLocationData location)
        {
            Current = location;
            ActiveRoom = null;

            foreach (HubRoom room in AllRooms())
            {
                bool isHere = location != null && room.Location == location;
                room.gameObject.SetActive(isHere);
                if (isHere) ActiveRoom = room;
            }

            if (location != null && ActiveRoom == null)
                Debug.LogError($"[HubLocationHost] No room in the scene is '{location.LocationId}'.", this);
        }

        public void Clear() => Show(null);
    }
}
