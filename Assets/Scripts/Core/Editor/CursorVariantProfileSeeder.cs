using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Cursor;

namespace ProjectAstra.Core.Editor
{
    // Seeds the four starter cursor variants so their numbers live in source rather than only
    // in four hand-edited assets. Creates what's missing and leaves what already exists alone
    // — a designer's tuning is never overwritten by re-running this.
    public static class CursorVariantProfileSeeder
    {
        private const string Folder = "Assets/ScriptableObjects/UI/CursorVariants";

        [MenuItem("Project Astra/UI/Seed Cursor Variant Profiles")]
        public static void Seed()
        {
            EnsureFolder();

            SeedHeirloom();
            SeedPetals();
            SeedBracketCompass();
            SeedMorphingCompass();

            AssetDatabase.SaveAssets();
            Debug.Log("[CursorVariantProfileSeeder] Starter cursor variants are in " + Folder + ".");
        }

        // 1 — brackets only. Idle breathes; selectable tightens and brightens; selected clamps
        // shut and goes still; acted greys out and stops dead.
        private static void SeedHeirloom()
        {
            var profile = GetOrCreate("GBAHeirloom");
            if (profile == null) return;

            var so = new SerializedObject(profile);
            so.FindProperty("displayName").stringValue = "GBA Heirloom";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = false;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = false;

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.92f), 0.44f, 1f, 0.05f, 2.2f, false);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.34f, 1.05f, 0.035f, 1.3f, false);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.27f, 1.05f, 0f, 1.3f, false);
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.7f), 0.44f, 0.95f, 0f, 2.2f, false);
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.46f, 1.05f, 0.055f, 0.9f, false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        // 2 — arrows only, never a closed border. Selectable flips them inward at the unit;
        // acted retracts them entirely; selected shows only the steps the unit can take.
        private static void SeedPetals()
        {
            var profile = GetOrCreate("CompassPetals");
            if (profile == null) return;

            var so = new SerializedObject(profile);
            so.FindProperty("displayName").stringValue = "Compass Petals";
            so.FindProperty("useCornerBrackets").boolValue = false;
            so.FindProperty("useEdgeArrows").boolValue = true;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = true;

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.9f), 0.46f, 0.9f, 0.05f, 2f, false);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.42f, 0.95f, 0.03f, 1.2f, true);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.46f, 0.9f, 0.02f, 1.6f, false);
            // Retracted: pulled to the centre, shrunk, and faded out.
            WriteState(so, "acted", new Color(0.5f, 0.5f, 0.55f, 0f), 0.1f, 0.6f, 0f, 2f, false);
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.48f, 1f, 0.06f, 0.85f, false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        // 3 — composite. Shorter bracket arms and slightly smaller arrows so eight elements on
        // one tile don't clutter, and one shared breath period so it reads as a single object.
        private static void SeedBracketCompass()
        {
            var profile = GetOrCreate("BracketCompass");
            if (profile == null) return;

            var so = new SerializedObject(profile);
            so.FindProperty("displayName").stringValue = "Bracket Compass";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = true;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = true;

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.88f), 0.45f, 0.78f, 0.04f, 2.2f, false);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.36f, 0.82f, 0.028f, 1.3f, true);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.3f, 0.8f, 0f, 1.3f, false);
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.68f), 0.45f, 0.74f, 0f, 2.2f, false);
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.47f, 0.82f, 0.05f, 0.9f, false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        // 4 — four pieces that shape-shift. Brackets at the corners over an empty tile; over a
        // unit they sweep along the edges and become arrows.
        private static void SeedMorphingCompass()
        {
            var profile = GetOrCreate("MorphingCompass");
            if (profile == null) return;

            var so = new SerializedObject(profile);
            so.FindProperty("displayName").stringValue = "Morphing Compass";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = false;
            so.FindProperty("useMorph").boolValue = true;
            so.FindProperty("hideInvalidDirections").boolValue = true;
            so.FindProperty("morphDuration").floatValue = 0.105f;

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.9f), 0.44f, 0.95f, 0.045f, 2.2f, false);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.42f, 1f, 0.03f, 1.3f, true);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.46f, 0.95f, 0.02f, 1.5f, false);
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.6f), 0.34f, 0.8f, 0f, 2.2f, false);
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.46f, 1f, 0.055f, 0.85f, false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        private static void WriteState(SerializedObject so, string field, Color tint, float inset,
            float scale, float amplitude, float period, bool inward)
        {
            var state = so.FindProperty(field);
            state.FindPropertyRelative("tint").colorValue = tint;
            state.FindPropertyRelative("inset").floatValue = inset;
            state.FindPropertyRelative("pieceScale").floatValue = scale;
            state.FindPropertyRelative("breathAmplitude").floatValue = amplitude;
            state.FindPropertyRelative("breathPeriod").floatValue = period;
            state.FindPropertyRelative("arrowsPointInward").boolValue = inward;
        }

        private static CursorVariantProfile GetOrCreate(string assetName)
        {
            string path = $"{Folder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CursorVariantProfile>(path);
            if (existing != null)
            {
                Debug.Log($"[CursorVariantProfileSeeder] {assetName} already exists — left untouched.");
                return null;
            }

            var profile = ScriptableObject.CreateInstance<CursorVariantProfile>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/UI"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "UI");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/UI", "CursorVariants");
        }
    }
}
