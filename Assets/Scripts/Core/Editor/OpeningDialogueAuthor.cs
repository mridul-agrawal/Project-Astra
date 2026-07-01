using System.IO;
using ProjectAstra.Core.Dialogue;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // One-shot author for the prototype's dialogue data: text-speed settings, the
    // two opening speakers + their registry, and a short Option-A opening script.
    // The actual opening lines are DEFERRED content (see the prototype design doc) —
    // this is a representative excerpt to wire and verify the system. Re-runnable.
    public static class OpeningDialogueAuthor
    {
        const string Root = "Assets/ScriptableObjects/Dialogue";

        [MenuItem("Project Astra/Author Opening Dialogue Assets")]
        public static void Author()
        {
            EnsureFolders();

            CreateSettings($"{Root}/DialogueSettings.asset");
            var protagonist = CreateSpeaker($"{Root}/Speakers/Speaker_Protagonist.asset", "PROTAGONIST", "Aranya");
            var child = CreateSpeaker($"{Root}/Speakers/Speaker_Child.asset", "CHILD", "Village Child");
            CreateRegistry($"{Root}/DialogueSpeakerRegistry.asset", protagonist, child);
            CreateOpeningScript($"{Root}/Scripts/OPENING_CH01.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OpeningDialogueAuthor] Authored dialogue assets under " + Root);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/ScriptableObjects", "Dialogue");
            EnsureFolder(Root, "Speakers");
            EnsureFolder(Root, "Scripts");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void CreateSettings(string path)
        {
            var settings = ScriptableObject.CreateInstance<DialogueSettings>();
            ReplaceAsset(settings, path);
        }

        private static DialogueSpeaker CreateSpeaker(string path, string speakerId, string displayName)
        {
            var speaker = ScriptableObject.CreateInstance<DialogueSpeaker>();
            var so = new SerializedObject(speaker);
            so.FindProperty("_speakerId").stringValue = speakerId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            ReplaceAsset(speaker, path);
            return AssetDatabase.LoadAssetAtPath<DialogueSpeaker>(path);
        }

        private static void CreateRegistry(string path, params DialogueSpeaker[] speakers)
        {
            var registry = ScriptableObject.CreateInstance<DialogueSpeakerRegistry>();
            var so = new SerializedObject(registry);
            var list = so.FindProperty("_speakers");
            list.arraySize = speakers.Length;
            for (int i = 0; i < speakers.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = speakers[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            ReplaceAsset(registry, path);
        }

        private static void CreateOpeningScript(string path)
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            var so = new SerializedObject(script);
            so.FindProperty("_scriptId").stringValue = "OPENING_CH01";

            var nodes = so.FindProperty("_nodes");
            nodes.arraySize = OpeningLines.Length;
            for (int i = 0; i < OpeningLines.Length; i++)
                ApplyLine(nodes.GetArrayElementAtIndex(i), i, OpeningLines[i]);

            so.ApplyModifiedPropertiesWithoutUndo();
            ReplaceAsset(script, path);
        }

        private static void ApplyLine(SerializedProperty node, int id, Line line)
        {
            node.FindPropertyRelative("_nodeId").intValue = id;
            node.FindPropertyRelative("_speakerId").stringValue = line.SpeakerId;
            node.FindPropertyRelative("_text").stringValue = line.Text;
            node.FindPropertyRelative("_expression").enumValueIndex = (int)line.Expression;
            node.FindPropertyRelative("_portraitPosition").enumValueIndex = (int)line.Position;
            node.FindPropertyRelative("_textSpeedOverride").floatValue = -1f;
            node.FindPropertyRelative("_autoAdvanceDelay").floatValue = 0f;
        }

        private static void ReplaceAsset(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private readonly struct Line
        {
            public readonly string SpeakerId;
            public readonly string Text;
            public readonly DialogueExpression Expression;
            public readonly PortraitPosition Position;

            public Line(string speakerId, string text, DialogueExpression expression, PortraitPosition position)
            {
                SpeakerId = speakerId;
                Text = text;
                Expression = expression;
                Position = position;
            }
        }

        private static readonly Line[] OpeningLines =
        {
            new(DialogueSpeakerRegistry.NarratorId, "Far from the village, deep in the forest, she gathered what the day had left to give.", DialogueExpression.Neutral, PortraitPosition.None),
            new("PROTAGONIST", "These berries should be enough for tonight.", DialogueExpression.Neutral, PortraitPosition.Left),
            new(DialogueSpeakerRegistry.NarratorId, "A scream tore through the trees.", DialogueExpression.Neutral, PortraitPosition.None),
            new("PROTAGONIST", "That came from home — no...", DialogueExpression.Surprised, PortraitPosition.Left),
            new("CHILD", "Help! Please — they're here! Monsters!", DialogueExpression.Afraid, PortraitPosition.Right),
            new("PROTAGONIST", "Rakshasas. Stay behind me. Where is everyone?", DialogueExpression.Determined, PortraitPosition.Left),
            new("CHILD", "At the temple! The gatekeeper is fighting them, but they're losing!", DialogueExpression.Afraid, PortraitPosition.Right),
            new("PROTAGONIST", "Then I clear the village first, and reach the temple after. Not one more villager falls.", DialogueExpression.Determined, PortraitPosition.Left),
        };
    }
}
