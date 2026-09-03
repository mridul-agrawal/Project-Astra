using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Holds whichever room is currently loaded, one at a time.
    public sealed class HubLocationHost : MonoBehaviour
    {
        private GameObject current;

        public HubLocationData Current { get; private set; }

        // Whatever a designer left sitting here is a preview, so the scene shows a real room
        // instead of an empty transform. The visit's own room replaces it the moment play starts.
        //
        // Immediate, because a deferred destroy would leave the preview's colliders in the world
        // for the rest of the frame - long enough for her to spawn inside one.
        private void Awake()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

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
