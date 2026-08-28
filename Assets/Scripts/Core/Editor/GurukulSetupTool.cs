using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Scenes;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Editor
{
    // Teaches the three shipped ScriptableObject assets about the Gurukul state. Code defaults only
    // seed brand-new assets, so the ones already on disk have to be patched by hand — and patched
    // additively, because regenerating them would drop every row a designer added since.
    //
    // Safe to run repeatedly: each step checks before it writes and reports only what it changed.
    public static class GurukulSetupTool
    {
        private const string HubScenePath = "Assets/Scenes/Gurukul.unity";
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";

        [MenuItem("Project Astra/Gurukul/Run Setup")]
        public static void RunSetup()
        {
            var changes = new List<string>();

            SyncTransitionTable(changes);
            SyncSceneStateCatalog(changes);
            SyncInputContextTable(changes);
            RegisterHubScene(changes);

            AssetDatabase.SaveAssets();
            ReportOutcome(changes);
        }

        private static void ReportOutcome(List<string> changes)
        {
            if (changes.Count == 0)
            {
                Debug.Log("[GurukulSetup] Everything already wired — no changes.");
                return;
            }
            Debug.Log($"[GurukulSetup] Applied {changes.Count} change(s):\n  " + string.Join("\n  ", changes));
        }

        // Appends every transition the code defaults declare but the asset is missing. Deliberately
        // never removes: the asset is the shipped truth and may hold rows the seed doesn't.
        private static void SyncTransitionTable(List<string> changes)
        {
            var table = LoadSingle<GameStateTransitionTable>();
            if (table == null) return;

            var serialized = new SerializedObject(table);
            SerializedProperty list = serialized.FindProperty("validTransitions");

            foreach (var entry in GameStateTransitionTable.CreateDefaultTransitions())
            {
                if (ContainsTransition(list, entry.From, entry.To)) continue;
                AppendTransition(list, entry.From, entry.To);
                changes.Add($"TransitionTable += {entry.From} -> {entry.To}");
            }

            serialized.ApplyModifiedProperties();
        }

        private static bool ContainsTransition(SerializedProperty list, GameState from, GameState to)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("From").enumValueIndex == (int)from &&
                    entry.FindPropertyRelative("To").enumValueIndex == (int)to)
                    return true;
            }
            return false;
        }

        private static void AppendTransition(SerializedProperty list, GameState from, GameState to)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("From").enumValueIndex = (int)from;
            added.FindPropertyRelative("To").enumValueIndex = (int)to;
        }

        // Without this row SceneLoader quietly ignores the state and no scene ever loads.
        private static void SyncSceneStateCatalog(List<string> changes)
        {
            var catalog = LoadSingle<SceneStateCatalog>();
            if (catalog == null || catalog.HasScene(GameState.Gurukul)) return;

            var serialized = new SerializedObject(catalog);
            SerializedProperty states = serialized.FindProperty("sceneStates");
            states.InsertArrayElementAtIndex(states.arraySize);
            states.GetArrayElementAtIndex(states.arraySize - 1).enumValueIndex = (int)GameState.Gurukul;
            serialized.ApplyModifiedProperties();

            changes.Add($"SceneStateCatalog += {GameState.Gurukul}");
        }

        // A state with no row here gets an empty allow-mask, so every input is disabled and nothing
        // logs. The mask comes from the code defaults so the two can't drift.
        private static void SyncInputContextTable(List<string> changes)
        {
            var table = LoadSingle<InputContextTable>();
            if (table == null || table.AllowedFor(GameState.Gurukul) != GameInputAction.None) return;

            GameInputAction mask = DefaultMaskFor(GameState.Gurukul);
            var serialized = new SerializedObject(table);
            SerializedProperty rules = serialized.FindProperty("rules");

            rules.InsertArrayElementAtIndex(rules.arraySize);
            SerializedProperty added = rules.GetArrayElementAtIndex(rules.arraySize - 1);
            added.FindPropertyRelative("state").enumValueIndex = (int)GameState.Gurukul;
            added.FindPropertyRelative("allowed").intValue = (int)mask;
            serialized.ApplyModifiedProperties();

            changes.Add($"InputContextTable += {GameState.Gurukul} allowing {mask}");
        }

        private static GameInputAction DefaultMaskFor(GameState state)
        {
            var defaults = ScriptableObject.CreateInstance<InputContextTable>();
            GameInputAction mask = defaults.AllowedFor(state);
            Object.DestroyImmediate(defaults);
            return mask;
        }

        // GameFlow resolves a campaign step's visitId through this catalog, so without it a hub step
        // silently finds no visit and falls back to whatever the scene has wired.
        //
        // Its own command because it opens and saves BootScene, which is too surprising to do as a
        // side effect of the other checks.
        [MenuItem("Project Astra/Gurukul/Wire Visit Catalog Into BootScene")]
        public static void WireVisitCatalog()
        {
            string reopen = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene boot = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var flow = Object.FindAnyObjectByType<GameFlow>();
            if (flow == null)
            {
                Debug.LogError("[GurukulSetup] No GameFlow in BootScene.");
                return;
            }

            var serialized = new SerializedObject(flow);
            SerializedProperty catalog = serialized.FindProperty("visitCatalog");
            if (catalog.objectReferenceValue == null)
            {
                catalog.objectReferenceValue = LoadSingle<GurukulVisitCatalog>();
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(boot);
                EditorSceneManager.SaveScene(boot);
                Debug.Log("[GurukulSetup] Wired the visit catalog into GameFlow.");
            }

            if (!string.IsNullOrEmpty(reopen) && reopen != BootScenePath)
                EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);
        }

        private static void RegisterHubScene(List<string> changes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HubScenePath) == null) return;

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == HubScenePath)) return;

            scenes.Add(new EditorBuildSettingsScene(HubScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            changes.Add($"BuildSettings += {HubScenePath}");
        }

        private static T LoadSingle<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                Debug.LogError($"[GurukulSetup] No {typeof(T).Name} asset found — skipping that step.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
