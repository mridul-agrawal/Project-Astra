using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Holds whichever room is currently loaded, one at a time.
    public sealed class HubLocationHost : MonoBehaviour
    {
        private GameObject current;

        public HubLocationData Current { get; private set; }

        // What a room's contents hang off, so anything added to a room is torn down with it.
        public Transform Room => current != null ? current.transform : transform;

        public void Show(HubLocationData location)
        {
            Clear();
            if (location == null) return;

            Current = location;
            if (location.RoomPrefab == null)
            {
                Debug.LogError($"[HubLocationHost] '{location.LocationId}' has no room prefab.", this);
                return;
            }

            current = Instantiate(location.RoomPrefab, transform, false);
            current.name = location.LocationId;
        }

        public void Clear()
        {
            if (current != null) Destroy(current);
            current = null;
            Current = null;
        }

    }
}
