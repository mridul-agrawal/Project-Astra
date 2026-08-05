using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Cursor.Debugging;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Two jobs. One: attach the variant visual system to whatever GridCursor is in the open
    // scene. Two: build CursorLab, the evaluation scene, by copying BattleMap and swapping its
    // roster for the 5-allies-3-spent-2-enemies scenario.
    //
    // Copying BattleMap rather than assembling a scene from nothing means the lab inherits all
    // the wiring — event service, turn manager, camera, map, HUD — instead of quietly
    // diverging from the real thing.
    public static class CursorLabSceneBuilder
    {
        private const string BattleMapPath = "Assets/Scenes/BattleMap.unity";
        private const string LabPath = "Assets/Scenes/CursorLab.unity";
        private const string VariantFolder = "Assets/ScriptableObjects/UI/CursorVariants";

        // Order matters — it's what the 1-4 hotkeys map onto.
        private static readonly string[] VariantOrder =
        {
            "GBAHeirloom", "CompassPetals", "BracketCompass", "MorphingCompass",
        };

        [MenuItem("Project Astra/UI/Attach Cursor Visuals To Open Scene")]
        public static void AttachToOpenScene()
        {
            var cursor = Object.FindAnyObjectByType<GridCursor>();
            if (cursor == null)
            {
                Debug.LogError("[CursorLabSceneBuilder] No GridCursor in the open scene.");
                return;
            }

            AttachVisuals(cursor);
            EditorSceneManager.MarkSceneDirty(cursor.gameObject.scene);
            Debug.Log("[CursorLabSceneBuilder] Cursor visuals attached. Save the scene to keep it.");
        }

        [MenuItem("Project Astra/UI/Build Cursor Lab Scene")]
        public static void BuildLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.DeleteAsset(LabPath);
            if (!AssetDatabase.CopyAsset(BattleMapPath, LabPath))
            {
                Debug.LogError($"[CursorLabSceneBuilder] Could not copy {BattleMapPath}.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);

            var cursor = Object.FindAnyObjectByType<GridCursor>();
            if (cursor != null) AttachVisuals(cursor);

            StripAuthoredUnits();
            AddLabHarness();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CursorLabSceneBuilder] CursorLab built. It is deliberately NOT in build settings.");
        }

        // The director owns its own child so the pieces can lag the logical position without
        // the root — which logic and the camera both read — ever being touched.
        private static void AttachVisuals(GridCursor cursor)
        {
            var existing = cursor.GetComponentInChildren<CursorVisualDirector>();
            var host = existing != null
                ? existing.gameObject
                : NewChild(cursor.transform, "CursorVisuals");

            var director = existing != null ? existing : host.AddComponent<CursorVisualDirector>();

            var so = new SerializedObject(director);
            so.FindProperty("cursor").objectReferenceValue = cursor;

            var profiles = so.FindProperty("profiles");
            profiles.ClearArray();
            for (int i = 0; i < VariantOrder.Length; i++)
            {
                var profile = AssetDatabase.LoadAssetAtPath<CursorVariantProfile>(
                    $"{VariantFolder}/{VariantOrder[i]}.asset");
                if (profile == null) continue;

                profiles.InsertArrayElementAtIndex(profiles.arraySize);
                profiles.GetArrayElementAtIndex(profiles.arraySize - 1).objectReferenceValue = profile;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            DisableLegacyCursorSprite(cursor);
        }

        // The old single-sprite cursor would draw underneath the new pieces. GridCursor still
        // uses the renderer's enabled flag to hide the cursor wholesale while locked, so the
        // component stays — only its sprite goes.
        private static void DisableLegacyCursorSprite(GridCursor cursor)
        {
            var so = new SerializedObject(cursor);
            var renderer = so.FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer;
            if (renderer == null) return;

            var rendererSo = new SerializedObject(renderer);
            rendererSo.FindProperty("m_Sprite").objectReferenceValue = null;
            rendererSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StripAuthoredUnits()
        {
            foreach (var unit in Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                Object.DestroyImmediate(unit.gameObject);
        }

        private static void AddLabHarness()
        {
            var harness = GameObject.Find("CursorLab") ?? new GameObject("CursorLab");

            if (harness.GetComponent<CursorLabBootstrapper>() == null)
                ConfigureBootstrapper(harness.AddComponent<CursorLabBootstrapper>());

            if (harness.GetComponent<CursorDebugOverlay>() == null)
                harness.AddComponent<CursorDebugOverlay>();
        }

        private static void ConfigureBootstrapper(CursorLabBootstrapper bootstrapper)
        {
            var so = new SerializedObject(bootstrapper);

            var ally = FirstDefinition("Assets/ScriptableObjects/Units/Characters");
            if (ally != null) so.FindProperty("allyDefinition").objectReferenceValue = ally;

            var enemy = FirstDefinition("Assets/ScriptableObjects/Units/Enemies") ?? ally;
            if (enemy != null) so.FindProperty("enemyDefinition").objectReferenceValue = enemy;

            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Cursor/PlaceholderUnitCircle.png");
            if (placeholder != null) so.FindProperty("placeholderSprite").objectReferenceValue = placeholder;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UnitDefinition FirstDefinition(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return null;

            return AssetDatabase.FindAssets("t:UnitDefinition", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<UnitDefinition>)
                .FirstOrDefault(definition => definition != null);
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
