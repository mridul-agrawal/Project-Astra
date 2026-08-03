using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAstra.Core.Editor
{
    // Keeps the Environment Builder's editor-only backdrop from ever leaving the
    // builder scene. The backdrop carries HideFlags.DontSave, so leaving the builder
    // scene doesn't destroy it — Unity orphans it scene-less (DontDestroyOnLoad-like),
    // and from there it survives into Play and renders on every scene (the black
    // square seen on the splash). This guard is always on — it clears the backdrop
    // whenever you leave the builder scene or enter Play, whether or not the
    // Environment Builder window is open. Pressing Play while still in the builder
    // scene ("preview") keeps the backdrop, which is the one place it should show.
    [InitializeOnLoad]
    public static class EnvironmentBackdropGuard
    {
        public const string BackdropName = "__Backdrop (editor only)";
        public const string BuilderScenePath = "Assets/Scenes/MapEnvironmentBuilder.unity";

        static EnvironmentBackdropGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

            // A stray may already be alive from before this guard loaded (e.g. the leak
            // that prompted this). Sweep it once the reload settles, unless we're
            // actively authoring in the builder scene.
            EditorApplication.delayCall += () => { if (!InBuilderScene()) ClearAllBackdrops(); };
        }

        // A backdrop must never cross into Play from anywhere but the builder scene,
        // where "Press Play to preview" legitimately keeps it visible.
        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode && !InBuilderScene())
                ClearAllBackdrops();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!InBuilderScene()) ClearAllBackdrops();
        }

        private static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (next.path != BuilderScenePath) ClearAllBackdrops();
        }

        // Destroys every editor-only backdrop, including a scene-less orphan that
        // GameObject.Find would miss. Shared with the builder window's pre-load clear.
        public static void ClearAllBackdrops()
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == BackdropName && !EditorUtility.IsPersistent(go))
                    Object.DestroyImmediate(go);
        }

        public static bool InBuilderScene() =>
            SceneManager.GetActiveScene().path == BuilderScenePath;
    }
}
