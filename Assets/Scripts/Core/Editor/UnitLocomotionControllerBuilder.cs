using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Builds the one shared unit locomotion state machine: Idle, a 4-direction
    // Run blend tree, and Selected, driven by MoveX / MoveY / Moving / Selected.
    // Every character reuses this graph and swaps in its own art through an
    // override controller (see UnitAnimatorSetupTool), so this runs once.
    public static class UnitLocomotionControllerBuilder
    {
        public const string ControllerPath = "Assets/Animation/UnitLocomotion.controller";

        [MenuItem("Project Astra/Animation/Build Unit Locomotion Controller")]
        public static void Build()
        {
            EnsureFolder("Assets/Animation");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameters(controller);
            BuildStates(controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;
            Debug.Log($"[Animation] Built {ControllerPath}");
        }

        private static void AddParameters(AnimatorController controller)
        {
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Selected", AnimatorControllerParameterType.Bool);
        }

        private static void BuildStates(AnimatorController controller)
        {
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = machine.AddState("Idle");
            idle.motion = NewLoopingClip("idle", controller);

            AnimatorState selected = machine.AddState("Selected");
            selected.motion = NewLoopingClip("selected", controller);

            AnimatorState run = machine.AddState("Run");
            run.motion = BuildRunBlendTree(controller);

            machine.defaultState = idle;

            LinkIdleAndRun(idle, run);
            LinkSelected(idle, run, selected);
        }

        private static BlendTree BuildRunBlendTree(AnimatorController controller)
        {
            var tree = new BlendTree
            {
                name = "Run",
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(tree, controller);

            tree.AddChild(NewLoopingClip("run_n", controller), new Vector2(0f, 1f));
            tree.AddChild(NewLoopingClip("run_e", controller), new Vector2(1f, 0f));
            tree.AddChild(NewLoopingClip("run_s", controller), new Vector2(0f, -1f));
            tree.AddChild(NewLoopingClip("run_w", controller), new Vector2(-1f, 0f));
            return tree;
        }

        // Empty placeholder clips define the override slots an AOC later fills.
        private static AnimationClip NewLoopingClip(string name, Object owner)
        {
            var clip = new AnimationClip { name = name };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.AddObjectToAsset(clip, owner);
            return clip;
        }

        private static void LinkIdleAndRun(AnimatorState idle, AnimatorState run)
        {
            Instant(idle, run).AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            AnimatorStateTransition runToIdle = Instant(run, idle);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Selected");
        }

        private static void LinkSelected(AnimatorState idle, AnimatorState run, AnimatorState selected)
        {
            AnimatorStateTransition idleToSelected = Instant(idle, selected);
            idleToSelected.AddCondition(AnimatorConditionMode.If, 0f, "Selected");
            idleToSelected.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            Instant(selected, run).AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            AnimatorStateTransition selectedToIdle = Instant(selected, idle);
            selectedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Selected");
            selectedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            AnimatorStateTransition runToSelected = Instant(run, selected);
            runToSelected.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            runToSelected.AddCondition(AnimatorConditionMode.If, 0f, "Selected");
        }

        // Snappy, immediate transition — right for discrete pixel-art states.
        private static AnimatorStateTransition Instant(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            return transition;
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
