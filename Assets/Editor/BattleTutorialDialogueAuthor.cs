using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Turn;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // One-shot author + wiring for a demonstrative battle-map tutorial trigger:
    // on the FIRST player phase, play the Map 1 movement line. Creates the battle
    // event channel + the tutorial script, then wires a DialogueTriggerDriver into
    // whatever scene is currently open (open BattleMap first). Real Map 1 tutorial
    // content + the selection/move/pre-combat triggers come with the Map 1 ticket.
    public static class BattleTutorialDialogueAuthor
    {
        const string Root = "Assets/ScriptableObjects/Dialogue";
        const string ChannelPath = Root + "/BattleDialogueEventChannel.asset";
        const string ScriptPath = Root + "/Scripts/MAP1_T1_MOVE.asset";
        const string TurnChannelPath = "Assets/ScriptableObjects/Core/TurnEventChannel.asset";

        [MenuItem("Project Astra/Author + Wire Battle Tutorial Trigger")]
        public static void Run()
        {
            var channel = LoadOrCreateChannel();
            var script = CreateTutorialScript();
            WireDriverIntoOpenScene(channel, script);
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleTutorialDialogueAuthor] Channel + MAP1_T1_MOVE authored and driver wired into the open scene.");
        }

        private static BattleDialogueEventChannel LoadOrCreateChannel()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BattleDialogueEventChannel>(ChannelPath);
            if (existing != null) return existing;

            var channel = ScriptableObject.CreateInstance<BattleDialogueEventChannel>();
            AssetDatabase.CreateAsset(channel, ChannelPath);
            return channel;
        }

        private static DialogueScript CreateTutorialScript()
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            var so = new SerializedObject(script);
            so.FindProperty("_scriptId").stringValue = "MAP1_T1_MOVE";

            var nodes = so.FindProperty("_nodes");
            nodes.arraySize = 1;
            var node = nodes.GetArrayElementAtIndex(0);
            node.FindPropertyRelative("_nodeId").intValue = 0;
            node.FindPropertyRelative("_speakerId").stringValue = "PROTAGONIST";
            node.FindPropertyRelative("_text").stringValue =
                "I need to get near the village — I have to reach the people before the Rakshasas do.";
            node.FindPropertyRelative("_expression").enumValueIndex = (int)DialogueExpression.Determined;
            node.FindPropertyRelative("_portraitPosition").enumValueIndex = (int)PortraitPosition.Left;
            node.FindPropertyRelative("_textSpeedOverride").floatValue = -1f;
            node.FindPropertyRelative("_autoAdvanceDelay").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadAssetAtPath<Object>(ScriptPath) != null) AssetDatabase.DeleteAsset(ScriptPath);
            AssetDatabase.CreateAsset(script, ScriptPath);
            return AssetDatabase.LoadAssetAtPath<DialogueScript>(ScriptPath);
        }

        private static void WireDriverIntoOpenScene(BattleDialogueEventChannel channel, DialogueScript script)
        {
            var turnChannel = AssetDatabase.LoadAssetAtPath<TurnEventChannel>(TurnChannelPath);
            var driver = GetOrCreateDriver();

            var so = new SerializedObject(driver);
            so.FindProperty("_turnChannel").objectReferenceValue = turnChannel;
            so.FindProperty("_battleChannel").objectReferenceValue = channel;

            var triggers = so.FindProperty("_triggers");
            triggers.arraySize = 1;
            var trigger = triggers.GetArrayElementAtIndex(0);
            trigger.FindPropertyRelative("_event").enumValueIndex = (int)BattleDialogueEventType.PlayerPhaseStarted;
            trigger.FindPropertyRelative("_turnFilter").intValue = 1;
            trigger.FindPropertyRelative("_script").objectReferenceValue = script;
            trigger.FindPropertyRelative("_fireOnce").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(driver);

            WireCursorChannel(channel);
        }

        private static DialogueTriggerDriver GetOrCreateDriver()
        {
            var driver = Object.FindObjectOfType<DialogueTriggerDriver>();
            if (driver != null) return driver;

            var go = new GameObject("DialogueTriggerDriver");
            return go.AddComponent<DialogueTriggerDriver>();
        }

        private static void WireCursorChannel(BattleDialogueEventChannel channel)
        {
            var cursor = Object.FindObjectOfType<GridCursor>();
            if (cursor == null) { Debug.LogWarning("[BattleTutorialDialogueAuthor] No GridCursor in open scene; skipped cursor channel wiring."); return; }

            var so = new SerializedObject(cursor);
            so.FindProperty("_battleDialogueChannel").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cursor);
        }
    }
}
