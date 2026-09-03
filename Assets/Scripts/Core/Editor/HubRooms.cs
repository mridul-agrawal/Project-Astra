using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Finds the room that is a given place, whether or not the designer has the Hub scene open.
    public static class HubRooms
    {
        public const string HubScenePath = "Assets/Scenes/HubExploration.unity";

        private static Scene borrowed;
        private static int depth;

        // Reading rooms while the Hub scene is closed means opening it somewhere harmless. Wrap a
        // batch of lookups in this and it is opened once and closed after, or not at all if the
        // designer already has it open.
        public static IDisposable Read() => new Session();

        // The room object for this place, or null if the scene has none.
        public static GameObject Find(HubLocationData location)
        {
            if (location == null) return null;

            GameObject open = FindInLoadedScenes(location);
            if (open != null) return open;

            return depth > 0 ? FindIn(Borrowed(), location) : null;
        }

        public static HubRoom[] InLoadedScenes() =>
            UnityEngine.Object.FindObjectsByType<HubRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private static GameObject FindInLoadedScenes(HubLocationData location)
        {
            foreach (HubRoom room in InLoadedScenes())
                if (room.Location == location) return room.gameObject;
            return null;
        }

        private static GameObject FindIn(Scene scene, HubLocationData location)
        {
            if (!scene.IsValid()) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var room = root.GetComponent<HubRoom>();
                if (room != null && room.Location == location) return root;
            }
            return null;
        }

        private static Scene Borrowed()
        {
            if (!borrowed.IsValid()) borrowed = EditorSceneManager.OpenPreviewScene(HubScenePath);
            return borrowed;
        }

        private sealed class Session : IDisposable
        {
            public Session() => depth++;

            public void Dispose()
            {
                depth = Mathf.Max(0, depth - 1);
                if (depth > 0 || !borrowed.IsValid()) return;

                EditorSceneManager.ClosePreviewScene(borrowed);
                borrowed = default;
            }
        }
    }
}
