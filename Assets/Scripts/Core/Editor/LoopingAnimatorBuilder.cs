using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Turns a folder of frames into a looping clip plus a one-state controller
    // that plays it — the animator you drop on an environment decoration or a
    // base-layer patch (pair it with AmbientAnimator for phase offset). Frames
    // are ordered by the trailing number in their name.
    public static class LoopingAnimatorBuilder
    {
        public static AnimatorController BuildFromFolder(string name, string framesFolder, int fps, string outputFolder)
        {
            Sprite[] frames = LoadFramesSorted(framesFolder);
            if (frames.Length == 0)
            {
                Debug.LogWarning($"[Animation] No frames found under {framesFolder}.");
                return null;
            }
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
