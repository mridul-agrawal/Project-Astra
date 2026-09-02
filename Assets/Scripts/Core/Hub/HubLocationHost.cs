using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Holds whichever room is currently loaded. One at a time: the old one is destroyed and the new
    // one built from its asset.
    //
    // Rooms are instantiated on demand rather than all sitting in the scene, because six student
    // houses plus the Library and the Guru's Quarters would make a scene file nobody could merge —
    // and rather than loaded as scenes, because SceneLoader only loads in single mode and would
    // take the whole hub down with it at every doorway.
    public sealed class HubLocationHost : MonoBehaviour
    {
        private const string GroundSortingLayer = "Ground";

        private GameObject current;

        public HubLocationData Current { get; private set; }

        // What a room's contents hang off, so anything added to a room is torn down with it.
        public Transform Room => current != null ? current.transform : transform;

        public void Show(HubLocationData location)
        {
            Clear();
            if (location == null) return;

            Current = location;
            current = new GameObject(location.LocationId);
            current.transform.SetParent(transform, false);

            DrawBaseArt(location, current.transform);
            SpawnProps(location, current.transform);
        }

        public void Clear()
        {
            if (current != null) Destroy(current);
            current = null;
            Current = null;
        }

        // Placed at the origin so the sprite's own pivot decides the alignment — the same
        // arrangement MapRenderer uses for a battle map's base art.
        private static void DrawBaseArt(HubLocationData location, Transform parent)
        {
            if (location.BaseArt == null)
            {
                Debug.LogWarning($"[HubLocationHost] Location '{location.LocationId}' has no base art.");
                return;
            }

            var go = new GameObject("BaseArt");
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = location.BaseArt;
            renderer.sortingLayerName = GroundSortingLayer;
            renderer.sortingOrder = 0;
        }

        private static void SpawnProps(HubLocationData location, Transform parent)
        {
            if (location.PropsPrefab == null) return;
            Instantiate(location.PropsPrefab, parent, false).name = "Props";
        }
    }
}
