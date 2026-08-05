using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Cursor.Debugging;

namespace ProjectAstra.Core.Editor
{
    // Puts the cursor variant system into whatever battle scene is open: the visual director on
    // the cursor, every variant profile in its picker, and the dev overlay on its own root.
    //
    // Re-runnable. It refreshes the profile list rather than duplicating anything, so it is the
    // way to pick up a fifth variant after you author one.
    public static class CursorVariantSetup
    {
        private const string VariantFolder = "Assets/ScriptableObjects/UI/CursorVariants";

        // Order is what the 1-4 hotkeys map onto. Anything else found in the folder is appended
        // after these, so a new variant lands on 5 without editing this list.
        private static readonly string[] PreferredOrder =
        {
            "GBAHeirloom", "CompassPetals", "BracketCompass", "MorphingCompass",
        };

        [MenuItem("Project Astra/UI/Set Up Cursor Variants In Open Scene")]
        public static void SetUp()
        {
            var cursor = Object.FindAnyObjectByType<GridCursor>();
            if (cursor == null)
            {
                Debug.LogError("[CursorVariantSetup] No GridCursor in the open scene. Open BattleMap first.");
                return;
            }

            AttachVisuals(cursor);
            AttachOverlay();

            EditorSceneManager.MarkSceneDirty(cursor.gameObject.scene);
            Debug.Log("[CursorVariantSetup] Cursor variants ready. Save the scene to keep it.");
        }

        // The director owns its own child so the pieces can lag the logical position without the
        // root — which gameplay and the camera both read — ever being touched.
        private static void AttachVisuals(GridCursor cursor)
        {
            var director = cursor.GetComponentInChildren<CursorVisualDirector>();
            if (director == null)
            {
                var host = new GameObject("CursorVisuals");
                host.transform.SetParent(cursor.transform, false);
                director = host.AddComponent<CursorVisualDirector>();
            }

            var so = new SerializedObject(director);
            so.FindProperty("cursor").objectReferenceValue = cursor;
            WriteProfileList(so.FindProperty("profiles"));
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearLegacyCursorSprite(cursor);
        }

        private static void WriteProfileList(SerializedProperty profiles)
        {
            profiles.ClearArray();

            foreach (var profile in LoadProfilesInOrder())
            {
                profiles.InsertArrayElementAtIndex(profiles.arraySize);
                profiles.GetArrayElementAtIndex(profiles.arraySize - 1).objectReferenceValue = profile;
            }
        }

        private static System.Collections.Generic.List<CursorVariantProfile> LoadProfilesInOrder()
        {
            var ordered = new System.Collections.Generic.List<CursorVariantProfile>();

            foreach (string name in PreferredOrder)
            {
                var profile = AssetDatabase.LoadAssetAtPath<CursorVariantProfile>($"{VariantFolder}/{name}.asset");
                if (profile != null) ordered.Add(profile);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:CursorVariantProfile", new[] { VariantFolder }))
            {
                var profile = AssetDatabase.LoadAssetAtPath<CursorVariantProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile != null && !ordered.Contains(profile)) ordered.Add(profile);
            }

            return ordered;
        }

        // The old single-sprite cursor would draw underneath the new pieces. GridCursor still
        // uses the renderer's enabled flag to hide the cursor wholesale while locked, so the
        // component stays — only its sprite goes.
        private static void ClearLegacyCursorSprite(GridCursor cursor)
        {
            var so = new SerializedObject(cursor);
            if (so.FindProperty("spriteRenderer").objectReferenceValue is not SpriteRenderer renderer) return;

            var rendererSo = new SerializedObject(renderer);
            rendererSo.FindProperty("m_Sprite").objectReferenceValue = null;
            rendererSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // A bare root, not a Canvas child: BattleUIActivator disables every Canvas child by
        // project rule, which would leave the overlay's Update — and so its hotkeys — dead.
        private static void AttachOverlay()
        {
            if (Object.FindAnyObjectByType<CursorDebugOverlay>() != null) return;

            var go = GameObject.Find("CursorDevOverlay") ?? new GameObject("CursorDevOverlay");
            go.AddComponent<CursorDebugOverlay>();
        }
    }
}
