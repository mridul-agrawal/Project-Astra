using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Gurukul.Conversation;

namespace ProjectAstra.Core.Editor
{
    // Seeds obviously-placeholder dialogue so the hub systems have something to run before design
    // has written a word.
    //
    // Every line here says outright that it is a placeholder. The GDD is explicit that programmers
    // do not invent dialogue, so nothing produced by this file should ever survive into the demo —
    // it exists so that "does the conversation graph work" and "has design written the scene" stay
    // separate questions.
    //
    // Precedent: BattleTutorialDialogueAuthor does the same job for the battle map.
    public static class GurukulPlaceholderDialogue
    {
        private const string Narrator = "NARRATOR";

        public static DialogueScript Script(string folder, string scriptId, params string[] lines)
        {
            string path = $"{folder}/{scriptId}.asset";
            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>(path);
            if (script == null)
            {
                script = ScriptableObject.CreateInstance<DialogueScript>();
                AssetDatabase.CreateAsset(script, path);
            }

            var serialized = new SerializedObject(script);
            serialized.FindProperty("scriptId").stringValue = scriptId;
            WriteSingleSegment(serialized.FindProperty("segments"), lines);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(script);
            return script;
        }

        // One segment holding every line, which is all a placeholder needs — segments exist to
        // change background or crawl speed part-way through.
        private static void WriteSingleSegment(SerializedProperty segments, IReadOnlyList<string> lines)
        {
            segments.arraySize = 1;
            SerializedProperty segment = segments.GetArrayElementAtIndex(0);
            segment.FindPropertyRelative("background").objectReferenceValue = null;
            segment.FindPropertyRelative("textSpeed").floatValue = -1f;
            segment.FindPropertyRelative("autoAdvanceDelay").floatValue = 0f;

            SerializedProperty lineList = segment.FindPropertyRelative("lines");
            lineList.arraySize = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                SerializedProperty line = lineList.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speakerId").stringValue = Narrator;
                line.FindPropertyRelative("text").stringValue = lines[i];
                line.FindPropertyRelative("portraitPosition").enumValueIndex = (int)PortraitPosition.None;
            }
        }

        public static ConversationGraphData Graph(string folder, string conversationId, string entryNodeId,
            ConversationNode[] nodes, string repeatEntryNodeId = null)
        {
            string path = $"{folder}/Conversation_{conversationId}.asset";
            var graph = AssetDatabase.LoadAssetAtPath<ConversationGraphData>(path);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<ConversationGraphData>();
                AssetDatabase.CreateAsset(graph, path);
            }

            var serialized = new SerializedObject(graph);
            serialized.FindProperty("conversationId").stringValue = conversationId;
            serialized.FindProperty("entryNodeId").stringValue = entryNodeId;
            serialized.FindProperty("repeatEntryNodeId").stringValue = repeatEntryNodeId ?? "";
            WriteNodes(serialized.FindProperty("nodes"), nodes);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static void WriteNodes(SerializedProperty list, ConversationNode[] nodes)
        {
            list.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
                WriteNode(list.GetArrayElementAtIndex(i), nodes[i]);
        }

        private static void WriteNode(SerializedProperty entry, ConversationNode node)
        {
            entry.FindPropertyRelative("nodeId").stringValue = node.nodeId;
            entry.FindPropertyRelative("kind").enumValueIndex = (int)node.kind;
            entry.FindPropertyRelative("script").objectReferenceValue = node.script;
            entry.FindPropertyRelative("nextNodeId").stringValue = node.nextNodeId ?? "";
            entry.FindPropertyRelative("allowCancel").boolValue = node.allowCancel;
            entry.FindPropertyRelative("cancelNodeId").stringValue = node.cancelNodeId ?? "";
            entry.FindPropertyRelative("flagId").stringValue = node.flagId ?? "";
            WriteOptions(entry.FindPropertyRelative("options"), node.options);
        }

        private static void WriteOptions(SerializedProperty list, ConversationOption[] options)
        {
            int count = options?.Length ?? 0;
            list.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("optionId").stringValue = options[i].optionId ?? "";
                entry.FindPropertyRelative("label").stringValue = options[i].label ?? "";
                entry.FindPropertyRelative("nextNodeId").stringValue = options[i].nextNodeId ?? "";
                entry.FindPropertyRelative("askOnce").boolValue = options[i].askOnce;
                entry.FindPropertyRelative("repeatNodeId").stringValue = options[i].repeatNodeId ?? "";
            }
        }

        public static ConversationGraphDatabase Catalog(string folder, params ConversationGraphData[] graphs)
        {
            string path = $"{folder}/ConversationGraphDatabase.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ConversationGraphDatabase>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ConversationGraphDatabase>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("conversations");
            foreach (ConversationGraphData graph in graphs) AppendUnique(list, graph);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AppendUnique(SerializedProperty list, Object entry)
        {
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == entry) return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
        }
    }
}
