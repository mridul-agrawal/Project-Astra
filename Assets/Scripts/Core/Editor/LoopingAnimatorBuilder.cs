using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Turns frames into a looping clip + a one-state controller — the animator you
    // drop on an environment decoration or a base-layer patch. Frames are ordered
    // by the trailing number in their name.
    //
    // A decoration folder can hold SEVERAL independent decorations (three tree
    // variations, several flowers), each its own asset with one animation — unlike
    // a unit, which is one entity with many states in one controller. So
    // BuildEachFromFolder makes one controller PER base name; BuildFromFolder makes
    // a single one from the whole folder.
    public static class LoopingAnimatorBuilder
    {
        // One controller from every frame in the folder (a single-animation folder).
        public static AnimatorController BuildFromFolder(string name, string framesFolder, int fps, string outputFolder)
        {
            Sprite[] frames = LoadFramesSorted(framesFolder);
            if (frames.Length == 0)
            {
                Debug.LogWarning($"[Animation] No frames found under {framesFolder}.");
                return null;
            }
            return BuildFromSprites(name, frames, fps, outputFolder);
        }

        // One controller per base-name group — e.g. a Tree folder holding tree_green_*,
        // tree_red_*, leaf_falling_yellow_* yields a separate controller for each,
        // named after that base. Each is a distinct asset with one animation.
        public static List<AnimatorController> BuildEachFromFolder(string framesFolder, int fps, string outputFolder)
        {
            var built = new List<AnimatorController>();
            foreach (KeyValuePair<string, List<Sprite>> group in GroupByBaseName(LoadFramesSorted(framesFolder)))
            {
                AnimatorController controller = BuildFromSprites(group.Key, group.Value.ToArray(), fps, outputFolder);
                if (controller != null) built.Add(controller);
            }
            if (built.Count == 0) Debug.LogWarning($"[Animation] No frames found under {framesFolder}.");
            return built;
        }

        public static AnimatorController BuildFromSprites(string name, Sprite[] frames, int fps, string outputFolder)
        {
            if (frames == null || frames.Length == 0) return null;
            EnsureFolder(outputFolder);

            AnimationClip clip = BuildLoopingClip(name, frames, fps);
            AssetDatabase.CreateAsset(clip, $"{outputFolder}/{name}.anim");

            var controller = AnimatorController.CreateAnimatorControllerAtPath($"{outputFolder}/{name}.controller");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Groups frames by base name (everything before the trailing _<number>),
        // each group ordered by that trailing number.
        private static SortedDictionary<string, List<Sprite>> GroupByBaseName(Sprite[] sprites)
        {
            var groups = new SortedDictionary<string, List<Sprite>>();
            foreach (Sprite s in sprites)
            {
                string baseName = BaseName(s.name);
                if (!groups.TryGetValue(baseName, out List<Sprite> list)) { list = new List<Sprite>(); groups[baseName] = list; }
                list.Add(s);
            }
            foreach (List<Sprite> list in groups.Values)
                list.Sort((a, b) => TrailingNumber(a.name).CompareTo(TrailingNumber(b.name)));
            return groups;
        }

        private static string BaseName(string spriteName)
        {
            int underscore = spriteName.LastIndexOf('_');
            if (underscore <= 0) return spriteName;
            return int.TryParse(spriteName.Substring(underscore + 1), out _) ? spriteName.Substring(0, underscore) : spriteName;
        }

        private static AnimationClip BuildLoopingClip(string name, Sprite[] frames, int fps)
        {
            var clip = new AnimationClip { name = name, frameRate = fps };
            var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };

            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static Sprite[] LoadFramesSorted(string folderPath)
        {
            var sprites = new List<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folderPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }
            return sprites.OrderBy(s => TrailingNumber(s.name)).ToArray();
        }

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
