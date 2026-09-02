using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Editor
{
    // Normal boot costs a splash video, a keypress at the title, and the whole opening cutscene
    // before you reach the battle map. That is fine once and miserable forty times, which is
    // what tuning a cursor takes.
    //
    // This flips the one field that decides where boot lands. It edits BootScene and saves it,
    // so the change is visible in source control — deliberately, because leaving it on and
    // committing it would ship a game that opens on the battle map.
    public static class DevBootTarget
    {
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";
        private const string SkipIntroMenu = "Project Astra/Dev/Boot Straight To Battle Map";
        private const string HubMenu = "Project Astra/Dev/Boot Straight To Hub";
        private const string NormalBootMenu = "Project Astra/Dev/Restore Normal Boot";

        [MenuItem(SkipIntroMenu)]
        public static void BootToBattleMap() => SetInitialState(GameState.BattleMap);

        // The hub only works booted from here, not by pressing Play on Hub.unity — InputManager
        // and the rest of the persistent services live in BootScene, so walking would do nothing.
        [MenuItem(HubMenu)]
        public static void BootToHub() => SetInitialState(GameState.HubExploration);

        [MenuItem(NormalBootMenu)]
        public static void BootNormally() => SetInitialState(GameState.Splash);

        [MenuItem(SkipIntroMenu, validate = true)]
        private static bool CanBootToBattleMap() => CurrentInitialState() != GameState.BattleMap;

        [MenuItem(HubMenu, validate = true)]
        private static bool CanBootToHub() => CurrentInitialState() != GameState.HubExploration;

        [MenuItem(NormalBootMenu, validate = true)]
        private static bool CanBootNormally() => CurrentInitialState() != GameState.Splash;

        private static void SetInitialState(GameState state)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var previouslyOpen = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            var manager = Object.FindAnyObjectByType<GameStateManager>();
            if (manager == null)
            {
                Debug.LogError("[DevBootTarget] No GameStateManager in BootScene.");
                return;
            }

            var so = new SerializedObject(manager);
            so.FindProperty("initialState").enumValueIndex = (int)state;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (state == GameState.Splash)
            {
                Debug.Log("[DevBootTarget] Normal boot restored — splash first.");
            }
            else
            {
                Debug.LogWarning($"[DevBootTarget] Boot now lands on {state}, skipping splash, title "
                    + "and the opening cutscene. Run 'Restore Normal Boot' before you commit BootScene.");
            }

            if (!string.IsNullOrEmpty(previouslyOpen) && previouslyOpen != BootScenePath)
                EditorSceneManager.OpenScene(previouslyOpen, OpenSceneMode.Single);
        }

        private static GameState CurrentInitialState()
        {
            // Reads the YAML rather than opening the scene, so the menu's tick state costs
            // nothing to evaluate every time the menu is drawn.
            foreach (string line in System.IO.File.ReadLines(BootScenePath))
            {
                if (!line.TrimStart().StartsWith("initialState:")) continue;
                if (int.TryParse(line.Split(':')[1].Trim(), out int value)) return (GameState)value;
            }
            return GameState.Splash;
        }
    }
}
