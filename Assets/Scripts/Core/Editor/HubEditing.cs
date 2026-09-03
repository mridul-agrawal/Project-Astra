using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Which room the designer is working on, and what the scene view is showing them about it.
    //
    // Remembered per project rather than saved into the scene, so choosing a room to edit is never
    // a change anyone has to commit.
    public static class HubEditing
    {
        private const string RoomKey = "ProjectAstra.Hub.EditingRoom";
        private const string OverlayKey = "ProjectAstra.Hub.Overlays";

        [System.Flags]
        public enum Overlay
        {
            None = 0,
            Extent = 1 << 0,
            Blocking = 1 << 1,
            Interaction = 1 << 2,
            Spawns = 1 << 3,
            All = Extent | Blocking | Interaction | Spawns
        }

        public static event System.Action Changed;

        public static Overlay Overlays
        {
            get => (Overlay)EditorPrefs.GetInt(OverlayKey, (int)Overlay.All);
            set { EditorPrefs.SetInt(OverlayKey, (int)value); Raise(); }
        }

        public static bool Shows(Overlay overlay) => (Overlays & overlay) != 0;

        public static void Toggle(Overlay overlay) =>
            Overlays = Shows(overlay) ? Overlays & ~overlay : Overlays | overlay;

        // The room being worked on. Null means show everything, which is the state a designer who
        // has never opened the tools is in.
        public static HubRoom EditingRoom
        {
            get
            {
                string id = EditorPrefs.GetString(RoomKey, "");
                if (string.IsNullOrEmpty(id)) return null;

                foreach (HubRoom room in HubRooms.InLoadedScenes())
                    if (room.LocationId == id) return room;
                return null;
            }
        }

        // Shows one room and hides the rest, so the designer is never editing two places stacked on
        // top of each other.
        //
        // Hidden rather than switched off: whether a room is active is saved into the scene, so
        // isolating and then pressing save would otherwise commit the other rooms as turned off.
        public static void Isolate(HubRoom room)
        {
            EditorPrefs.SetString(RoomKey, room != null ? room.LocationId : "");

            SceneVisibilityManager visibility = SceneVisibilityManager.instance;
            foreach (HubRoom candidate in HubRooms.InLoadedScenes())
            {
                if (room == null || candidate == room) visibility.Show(candidate.gameObject, true);
                else visibility.Hide(candidate.gameObject, true);
            }

            if (room != null) Frame(room);
            Raise();
            SceneView.RepaintAll();
        }

        public static void ShowEveryRoom() => Isolate(null);

        // Puts the whole place on screen, so opening a room always starts from a view of all of it.
        public static void Frame(HubRoom room)
        {
            if (room == null || SceneView.lastActiveSceneView == null) return;

            Rect bounds = room.Location != null ? room.Location.Bounds : new Rect(0, 0, 15, 9);
            var centre = new Vector3(bounds.center.x, bounds.center.y, 0f);
            float size = Mathf.Max(bounds.width, bounds.height) * 0.62f;

            SceneView.lastActiveSceneView.LookAt(centre, Quaternion.identity, size, true, false);
        }

        private static void Raise() => Changed?.Invoke();
    }
}
