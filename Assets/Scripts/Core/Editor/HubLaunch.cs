using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Editor
{
    // Starts the game in the middle of a visit, so testing one conversation does not mean playing
    // everything before it.
    //
    // The hub only works booted from BootScene — the input manager and the rest of the persistent
    // services live there. So this opens it, tells it to open on the hub, plays, and then puts both
    // the boot target and the scene the designer was in back the way they were.
    public static class HubLaunch
    {
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";
        private const string BootTargetKey = "ProjectAstra.Hub.Launch.RestoreBootTo";
        private const string ReturnSceneKey = "ProjectAstra.Hub.Launch.ReturnTo";

        public static bool CanLaunch => !Application.isPlaying;

        [InitializeOnLoadMethod]
        private static void WatchForTheEnd() =>
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) PutEverythingBack();
            };

        // Play, starting in this visit, at this stage, optionally standing somewhere in particular.
        public static void PlayFrom(HubVisitData visit, int stage, Vector2? spawn)
        {
            if (visit == null || !CanLaunch) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().path);
            HubLaunchRequest.Set(visit.VisitId, stage, spawn);

            if (!SendBootToTheHub()) { Forget(); return; }

            EditorApplication.isPlaying = true;
        }

        private static bool SendBootToTheHub()
        {
            Scene boot = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            SerializedProperty opensOn = OpensOn(boot, out SerializedObject manager);
            if (opensOn == null) return false;

            if ((GameState)opensOn.enumValueIndex != GameState.HubExploration)
            {
                EditorPrefs.SetInt(BootTargetKey, opensOn.enumValueIndex);
                opensOn.enumValueIndex = (int)GameState.HubExploration;
                Save(manager, boot);
            }
            return true;
        }

        private static void PutEverythingBack()
        {
            PutBootBack();
            ReturnToTheSceneTheyWereIn();
            HubLaunchRequest.Clear();
        }

        private static void PutBootBack()
        {
            if (!EditorPrefs.HasKey(BootTargetKey)) return;

            int wanted = EditorPrefs.GetInt(BootTargetKey);
            EditorPrefs.DeleteKey(BootTargetKey);

            Scene boot = SceneManager.GetActiveScene().path == BootScenePath
                ? SceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            SerializedProperty opensOn = OpensOn(boot, out SerializedObject manager);
            if (opensOn == null) return;

            opensOn.enumValueIndex = wanted;
            Save(manager, boot);
        }

        private static void ReturnToTheSceneTheyWereIn()
        {
            string path = EditorPrefs.GetString(ReturnSceneKey, "");
            EditorPrefs.DeleteKey(ReturnSceneKey);

            if (string.IsNullOrEmpty(path) || path == SceneManager.GetActiveScene().path) return;
            if (System.IO.File.Exists(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void Forget()
        {
            HubLaunchRequest.Clear();
            EditorPrefs.DeleteKey(ReturnSceneKey);
        }

        private static SerializedProperty OpensOn(Scene boot, out SerializedObject manager)
        {
            GameStateManager found = boot.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<GameStateManager>(true))
                .FirstOrDefault(candidate => candidate != null);

            if (found == null)
            {
                Debug.LogError("[HubLaunch] No GameStateManager in BootScene, so there is nothing to tell.");
                manager = null;
                return null;
            }

            manager = new SerializedObject(found);
            return manager.FindProperty("initialState");
        }

        private static void Save(SerializedObject manager, Scene scene)
        {
            manager.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
