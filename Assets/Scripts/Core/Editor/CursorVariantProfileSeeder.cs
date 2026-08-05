using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Cursor;

namespace ProjectAstra.Core.Editor
{
    // Seeds the four starter cursor variants so their numbers live in source rather than only
    // in four hand-edited assets.
    //
    // Two entry points on purpose. Seed creates what's missing and never touches an existing
    // profile, so it's safe to run any time. Reset overwrites all four back to these values and
    // throws away tuning — it exists for getting back to a known baseline, and says so.
    public static class CursorVariantProfileSeeder
    {
        private const string Folder = "Assets/ScriptableObjects/UI/CursorVariants";

        [MenuItem("Project Astra/UI/Seed Cursor Variant Profiles")]
        public static void Seed() => Run(overwrite: false);

        [MenuItem("Project Astra/UI/Reset Cursor Variant Profiles To Defaults")]
        public static void Reset()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset cursor variants?",
                "This overwrites all four cursor variant profiles with their starting values. "
                + "Any tuning you've done to them is lost.",
                "Reset", "Cancel");

            if (confirmed) Run(overwrite: true);
        }

        private static void Run(bool overwrite)
        {
            EnsureFolder();

            SeedHeirloom(overwrite);
            SeedPetals(overwrite);
            SeedBracketCompass(overwrite);
            SeedMorphingCompass(overwrite);

            AssetDatabase.SaveAssets();
            Debug.Log($"[CursorVariantProfileSeeder] Starter cursor variants are in {Folder}.");
        }

        // 1 — chunky retro brackets, hard corners, no arrows. Idle breathes; selectable tightens
        // and brightens; selected clamps almost shut and goes still; acted thins out and stops.
        private static void SeedHeirloom(bool overwrite)
        {
            var so = Open("GBAHeirloom", overwrite);
            if (so == null) return;

            so.FindProperty("displayName").stringValue = "GBA Heirloom";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = false;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = false;

            var chunky = Shape(arm: 0.78f, thick: 0.3f, outline: 0.09f, radius: 0f);

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.92f), 0.44f, 1f, 0.05f, 2.2f, false, chunky);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.34f, 1.05f, 0.035f, 1.3f, false, chunky);
            // Clamped shut: the arms grow until the frame nearly closes.
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.27f, 1.05f, 0f, 1.3f, false,
                Shape(arm: 0.95f, thick: 0.32f, outline: 0.09f, radius: 0f));
            // Thinned out: same footprint, less presence.
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.7f), 0.44f, 0.95f, 0f, 2.2f, false,
                Shape(arm: 0.55f, thick: 0.18f, outline: 0.07f, radius: 0f));
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.46f, 1.05f, 0.055f, 0.9f, false, chunky);

            Apply(so);
        }

        // 2 — arrows only, never a closed border. Selectable flips them inward and blunts them;
        // acted retracts them to nothing.
        private static void SeedPetals(bool overwrite)
        {
            var so = Open("CompassPetals", overwrite);
            if (so == null) return;

            so.FindProperty("displayName").stringValue = "Compass Petals";
            so.FindProperty("useCornerBrackets").boolValue = false;
            so.FindProperty("useEdgeArrows").boolValue = true;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = true;

            var needle = Shape(arrowWidth: 0.5f, sharpness: 0.78f, outline: 0.08f, radius: 0.1f);

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.9f), 0.46f, 0.9f, 0.05f, 2f, false, needle);
            // Pointing in at the unit, wider and blunter so they read as a claim, not a flick.
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.42f, 0.95f, 0.03f, 1.2f, true,
                Shape(arrowWidth: 0.66f, sharpness: 0.55f, outline: 0.08f, radius: 0.15f));
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.46f, 0.9f, 0.02f, 1.6f, false, needle);
            // Retracted: pulled to the centre, shrunk, faded out, and narrowed to a sliver.
            WriteState(so, "acted", new Color(0.5f, 0.5f, 0.55f, 0f), 0.1f, 0.6f, 0f, 2f, false,
                Shape(arrowWidth: 0.2f, sharpness: 0.9f, outline: 0.05f, radius: 0.1f));
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.48f, 1f, 0.06f, 0.85f, false,
                Shape(arrowWidth: 0.44f, sharpness: 0.88f, outline: 0.09f, radius: 0.05f));

            Apply(so);
        }

        // 3 — composite. Everything finer and shorter, because eight elements on one tile is a
        // lot; one shared breath period so it reads as a single object.
        private static void SeedBracketCompass(bool overwrite)
        {
            var so = Open("BracketCompass", overwrite);
            if (so == null) return;

            so.FindProperty("displayName").stringValue = "Bracket Compass";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = true;
            so.FindProperty("useMorph").boolValue = false;
            so.FindProperty("hideInvalidDirections").boolValue = true;

            var fine = Shape(arm: 0.5f, thick: 0.16f, outline: 0.07f, radius: 0.2f,
                arrowWidth: 0.4f, sharpness: 0.7f);

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.88f), 0.45f, 0.78f, 0.04f, 2.2f, false, fine);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.36f, 0.82f, 0.028f, 1.3f, true, fine);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.3f, 0.8f, 0f, 1.3f, false,
                Shape(arm: 0.66f, thick: 0.18f, outline: 0.07f, radius: 0.2f, arrowWidth: 0.4f, sharpness: 0.7f));
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.68f), 0.45f, 0.74f, 0f, 2.2f, false,
                Shape(arm: 0.38f, thick: 0.13f, outline: 0.06f, radius: 0.2f, arrowWidth: 0.3f, sharpness: 0.7f));
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.47f, 0.82f, 0.05f, 0.9f, false, fine);

            Apply(so);
        }

        // 4 — four pieces that shape-shift, so the bracket and the arrow are deliberately
        // rounded and similar in mass; a hard-cornered bracket morphing into a needle reads as
        // two different objects rather than one changing its mind.
        private static void SeedMorphingCompass(bool overwrite)
        {
            var so = Open("MorphingCompass", overwrite);
            if (so == null) return;

            so.FindProperty("displayName").stringValue = "Morphing Compass";
            so.FindProperty("useCornerBrackets").boolValue = true;
            so.FindProperty("useEdgeArrows").boolValue = false;
            so.FindProperty("useMorph").boolValue = true;
            so.FindProperty("hideInvalidDirections").boolValue = true;
            so.FindProperty("morphDuration").floatValue = 0.105f;

            var rounded = Shape(arm: 0.7f, thick: 0.24f, outline: 0.08f, radius: 0.45f,
                arrowWidth: 0.56f, sharpness: 0.55f);

            WriteState(so, "idle", new Color(1f, 0.97f, 0.88f, 0.9f), 0.44f, 0.95f, 0.045f, 2.2f, false, rounded);
            WriteState(so, "selectable", new Color(1f, 0.86f, 0.36f, 1f), 0.42f, 1f, 0.03f, 1.3f, true, rounded);
            WriteState(so, "selected", new Color(0.5f, 0.88f, 1f, 1f), 0.46f, 0.95f, 0.02f, 1.5f, false, rounded);
            WriteState(so, "acted", new Color(0.48f, 0.48f, 0.53f, 0.6f), 0.34f, 0.8f, 0f, 2.2f, false,
                Shape(arm: 0.5f, thick: 0.18f, outline: 0.06f, radius: 0.45f, arrowWidth: 0.4f, sharpness: 0.55f));
            WriteState(so, "enemy", new Color(1f, 0.32f, 0.28f, 1f), 0.46f, 1f, 0.055f, 0.85f, false, rounded);

            Apply(so);
        }

        private static CursorShape Shape(float arm = 0.72f, float thick = 0.2f, float outline = 0.07f,
            float radius = 0.06f, float arrowWidth = 0.55f, float sharpness = 0.6f) => new()
        {
            armLength = arm,
            thickness = thick,
            outlineWeight = outline,
            cornerRadius = radius,
            arrowWidth = arrowWidth,
            arrowSharpness = sharpness,
        };

        private static void WriteState(SerializedObject so, string field, Color tint, float inset,
            float scale, float amplitude, float period, bool inward, CursorShape shape)
        {
            var state = so.FindProperty(field);
            state.FindPropertyRelative("tint").colorValue = tint;
            state.FindPropertyRelative("inset").floatValue = inset;
            state.FindPropertyRelative("pieceScale").floatValue = scale;
            state.FindPropertyRelative("breathAmplitude").floatValue = amplitude;
            state.FindPropertyRelative("breathPeriod").floatValue = period;
            state.FindPropertyRelative("arrowsPointInward").boolValue = inward;

            var shapeProperty = state.FindPropertyRelative("shape");
            shapeProperty.FindPropertyRelative("armLength").floatValue = shape.armLength;
            shapeProperty.FindPropertyRelative("thickness").floatValue = shape.thickness;
            shapeProperty.FindPropertyRelative("outlineWeight").floatValue = shape.outlineWeight;
            shapeProperty.FindPropertyRelative("cornerRadius").floatValue = shape.cornerRadius;
            shapeProperty.FindPropertyRelative("arrowWidth").floatValue = shape.arrowWidth;
            shapeProperty.FindPropertyRelative("arrowSharpness").floatValue = shape.arrowSharpness;
        }

        private static SerializedObject Open(string assetName, bool overwrite)
        {
            string path = $"{Folder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CursorVariantProfile>(path);

            if (existing != null && !overwrite)
            {
                Debug.Log($"[CursorVariantProfileSeeder] {assetName} already exists — left untouched.");
                return null;
            }

            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<CursorVariantProfile>();
                AssetDatabase.CreateAsset(existing, path);
            }

            return new SerializedObject(existing);
        }

        private static void Apply(SerializedObject so)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so.targetObject);
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
