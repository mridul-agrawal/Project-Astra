using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Turns a folder of a character's animation frames into the six locomotion
    // clips plus an Animator Override Controller wired to the shared base graph.
    // Frames are grouped by filename prefix (idle_*, run_n_*, run_e_*, run_s_*,
    // run_w_*, selected_*), so one click per character is enough — no hand
    // wiring of state machines. Drop the resulting override controller on the
    // character's UnitDefinition.mapAnimator.
    public sealed class UnitAnimatorSetupTool : EditorWindow
    {
        private static readonly string[] StateKeys = { "idle", "run_n", "run_e", "run_s", "run_w", "selected" };
        private const string OutputRoot = "Assets/Animation/Units";

        private string characterName = "";
        private DefaultAsset sourceFolder;
        private int frameRate = 10;

        [MenuItem("Project Astra/Animation/Unit Animator Builder")]
        public static void Open() => GetWindow<UnitAnimatorSetupTool>("Unit Animator Builder");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build a unit's 6 clips + override controller", EditorStyles.boldLabel);
            characterName = EditorGUILayout.TextField("Character Name", characterName);
            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Frames Folder", sourceFolder, typeof(DefaultAsset), false);
            frameRate = Mathf.Max(1, EditorGUILayout.IntField("Frame Rate (fps)", frameRate));

            EditorGUILayout.HelpBox(
                "Frames are grouped by name prefix: idle_*, run_n_*, run_e_*, run_s_*, run_w_*, selected_*.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!CanBuild()))
                if (GUILayout.Button("Build"))
                    BuildFrom(characterName, AssetDatabase.GetAssetPath(sourceFolder), frameRate);
        }

        private bool CanBuild() =>
            !string.IsNullOrWhiteSpace(characterName) &&
            sourceFolder != null &&
            BaseController() != null;

        // Programmatic entry point (also used by the placeholder generator). Returns
        // the override controller, or null if the base controller / frames are missing.
        public static AnimatorOverrideController BuildFrom(string characterName, string folderPath, int frameRate)
        {
            if (BaseController() == null)
            {
                Debug.LogError("[Animation] No base controller. Run 'Build Unit Locomotion Controller' first.");
                return null;
            }

            Dictionary<string, Sprite[]> framesByState = GroupFrames(folderPath);
            if (framesByState.Count == 0)
            {
                Debug.LogWarning($"[Animation] No frames found under {folderPath} matching the naming convention.");
                return null;
            }

            string outputFolder = $"{OutputRoot}/{characterName}";
            EnsureFolder(outputFolder);

            var clipsByState = new Dictionary<string, AnimationClip>();
            foreach (KeyValuePair<string, Sprite[]> entry in framesByState)
                clipsByState[entry.Key] = BuildClip(characterName, entry.Key, entry.Value, frameRate, outputFolder);

            AnimatorOverrideController aoc = BuildOverrideController(characterName, clipsByState, outputFolder);
            AssetDatabase.SaveAssets();
            Selection.activeObject = aoc;
            WarnAboutMissingStates(characterName, framesByState);
            Debug.Log($"[Animation] Built override controller for '{characterName}' at {outputFolder}");
            return aoc;
        }

        private static Dictionary<string, Sprite[]> GroupFrames(string folderPath)
        {
            List<Sprite> sprites = LoadSprites(folderPath);
            var grouped = new Dictionary<string, Sprite[]>();
            foreach (string state in StateKeys)
            {
                Sprite[] frames = sprites
                    .Where(s => s.name.StartsWith(state + "_"))
                    .OrderBy(s => TrailingNumber(s.name))
                    .ToArray();
                if (frames.Length > 0) grouped[state] = frames;
            }
            return grouped;
        }

        private static List<Sprite> LoadSprites(string folderPath)
        {
            var sprites = new List<Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }
            return sprites;
        }

        private static AnimationClip BuildClip(
            string characterName, string state, Sprite[] frames, int frameRate, string outputFolder)
        {
            var clip = new AnimationClip { name = state, frameRate = frameRate };
            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",   // Animator sits on the same object as the SpriteRenderer
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keyframes[i] = new ObjectReferenceKeyframe { time = i / (float)frameRate, value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, $"{outputFolder}/{characterName}_{state}.anim");
            return clip;
        }

        private static AnimatorOverrideController BuildOverrideController(
            string characterName, Dictionary<string, AnimationClip> clipsByState, string outputFolder)
        {
            var aoc = new AnimatorOverrideController(BaseController()) { name = characterName };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip baseClip = overrides[i].Key;
                if (clipsByState.TryGetValue(baseClip.name, out AnimationClip replacement))
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, replacement);
            }
            aoc.ApplyOverrides(overrides);

            AssetDatabase.CreateAsset(aoc, $"{outputFolder}/{characterName}.overrideController");
            return aoc;
        }

        private static void WarnAboutMissingStates(string characterName, Dictionary<string, Sprite[]> framesByState)
        {
            string[] missing = StateKeys.Where(s => !framesByState.ContainsKey(s)).ToArray();
            if (missing.Length > 0)
                Debug.LogWarning($"[Animation] '{characterName}' missing frames for: {string.Join(", ", missing)}. " +
                                 "Those states keep the placeholder clip.");
        }

        private static AnimatorController BaseController() =>
            AssetDatabase.LoadAssetAtPath<AnimatorController>(UnitLocomotionControllerBuilder.ControllerPath);

        // Sorts idle_2 before idle_10 by reading the number after the last '_'.
        private static int TrailingNumber(string name)
        {
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name[(underscore + 1)..], out int n) ? n : 0;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
