using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Writing a conversation, and seeing the shape it has.
    //
    // Branching is stored as labels and target labels, so the flow down the middle is a view of the
    // script rather than a second copy of it. Nothing here has to be kept in step with anything.
    public class HubConversationWindow : EditorWindow
    {
        private const float ListWidth = 200f;
        private const float LineWidth = 340f;
        private const float GutterWidth = 26f;

        private DialogueScript script;
        private SerializedObject editable;
        private string filter = "";
        private int segment = -1, line = -1;
        private Vector2 listScroll, flowScroll, lineScroll;
        private HubVisitData hearIn;

        [MenuItem("Project Astra/Conversations")]
        public static void Open()
        {
            var window = GetWindow<HubConversationWindow>("Conversations");
            window.minSize = new Vector2(900, 560);
        }

        public static void OpenOn(DialogueScript wanted)
        {
            var window = GetWindow<HubConversationWindow>("Conversations");
            window.Select(wanted);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                if (script == null) DrawNothingPicked();
                else { DrawFlow(); DrawLine(); }
            }
        }

        private static void DrawNothingPicked() =>
            EditorGUILayout.HelpBox("Pick a conversation on the left, or write a new one.", MessageType.None);

        // --- The list ---

        private void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    filter = EditorGUILayout.TextField(filter);
                    if (GUILayout.Button("+ New", GUILayout.Width(56))) WriteANewOne();
                }

                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (DialogueScript candidate in All().Where(Matches))
                    if (GUILayout.Toggle(script == candidate, candidate.ScriptId, "Button") && script != candidate)
                        Select(candidate);
                EditorGUILayout.EndScrollView();
            }
        }

        private void WriteANewOne()
        {
            string id = HubAuthoring.Unused("new_conversation", HubIds.Of(HubIdKind.Conversation));
            Select(HubAssets.Create(HubIdKind.Conversation, id) as DialogueScript);
        }

        // --- The flow ---

        private void DrawFlow()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawFlowHeader();

                flowScroll = EditorGUILayout.BeginScrollView(flowScroll);
                foreach (HubConversationFlow.Block block in HubConversationFlow.Read(script))
                    DrawBlock(block);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFlowHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(script.ScriptId, EditorStyles.boldLabel);
                if (GUILayout.Button("Reveal", GUILayout.Width(64))) EditorGUIUtility.PingObject(script);
            }

            DrawHearIt();

            foreach (string wrong in HubConversationFlow.Problems(script))
                EditorGUILayout.HelpBox(wrong, MessageType.Warning);
        }

        // Hearing it is playing it, in the real dialogue view, with the real speakers. Nothing else
        // proves a portrait is on the right side.
        private void DrawHearIt()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Hear it now")) PlayItNow();
                    return;
                }

                HubVisitData[] visits = HubVisitLens.All().ToArray();
                if (visits.Length == 0) return;

                int at = Mathf.Max(0, Array.IndexOf(visits, hearIn));
                hearIn = visits[EditorGUILayout.Popup(at, visits.Select(v => v.VisitId).ToArray())];

                using (new EditorGUI.DisabledScope(!HubLaunch.CanLaunch))
                    if (GUILayout.Button("Hear it", GUILayout.Width(90)))
                        HubLaunch.PlayFrom(hearIn, 0, null, script.ScriptId);
            }
        }

        private void PlayItNow() =>
            DialogueService.Instance?.Play(script, DialogueTriggeringContext.Conversation, null,
                HubVisitService.Instance?.Dialogue, Hub.Interaction.InteractionEvents.RaiseFlag);

        private void DrawBlock(HubConversationFlow.Block block)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(block.Name, EditorStyles.boldLabel);

                for (int i = block.First; i < block.First + block.Count; i++) DrawEntry(i);

                foreach (HubConversationFlow.Exit exit in block.Exits) DrawExit(exit);
            }
        }

        private void DrawEntry(int index)
        {
            DialogueNode node = script.Nodes[index];
            (int inSegment, int inLine) = Locate(index);

            bool picked = segment == inSegment && line == inLine;
            if (GUILayout.Toggle(picked, Reads(node), EditorStyles.miniButton) && !picked)
            {
                segment = inSegment;
                line = inLine;
            }
        }

        private static string Reads(DialogueNode node) => node.Kind switch
        {
            DialogueNodeKind.Line => $"{Speaker(node)}  {Short(node.Text)}",
            DialogueNodeKind.Choice => $"asks  ({node.Options.Count} answers)",
            DialogueNodeKind.Jump => $"goes to {node.TargetLabel}",
            DialogueNodeKind.Signal => $"says '{node.SignalId}' happened",
            _ => node.Kind.ToString()
        };

        private static string Speaker(DialogueNode node) =>
            string.IsNullOrEmpty(node.SpeakerId) ? "—" : node.SpeakerId;

        private static string Short(string text)
        {
            string one = (text ?? "").Replace("\n", " ");
            return one.Length <= 54 ? one : one[..52] + "…";
        }

        private void DrawExit(HubConversationFlow.Exit exit)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(GutterWidth);

                // An answer with nowhere to go ends the conversation, which the answer's own words
                // do not say.
                if (exit.Ends)
                {
                    EditorGUILayout.LabelField($"{exit.Reads}  ·  ends the conversation",
                        EditorStyles.miniLabel);
                    return;
                }

                Color was = GUI.color;
                if (exit.Dangling) GUI.color = new Color(1f, 0.6f, 0.4f);

                if (GUILayout.Button($"{exit.Reads}  →  {exit.TargetLabel}", EditorStyles.miniButton))
                    GoToLabel(exit.TargetLabel);

                GUI.color = was;
            }
        }

        private void GoToLabel(string label)
        {
            int at = script.IndexOfLabel(label);
            if (at < 0) return;

            (segment, line) = Locate(at);
        }

        // --- The one line being written ---

        private void DrawLine()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LineWidth)))
            {
                SerializedProperty entry = Entry();
                if (entry == null)
                {
                    EditorGUILayout.HelpBox("Pick a line to write it.", MessageType.None);
                    return;
                }

                editable.Update();
                lineScroll = EditorGUILayout.BeginScrollView(lineScroll);
                EditorGUILayout.PropertyField(entry, new GUIContent("This entry"), true);
                EditorGUILayout.EndScrollView();
                editable.ApplyModifiedProperties();
            }
        }

        private SerializedProperty Entry()
        {
            if (editable == null || segment < 0 || line < 0) return null;

            SerializedProperty segments = editable.FindProperty("segments");
            if (segment >= segments.arraySize) return null;

            SerializedProperty lines = segments.GetArrayElementAtIndex(segment).FindPropertyRelative("lines");
            return line < lines.arraySize ? lines.GetArrayElementAtIndex(line) : null;
        }

        // --- Lookups ---

        // Nodes are the segments flattened, so a position in the flow has to be turned back into
        // which segment and which line inside it.
        private (int, int) Locate(int nodeIndex)
        {
            SerializedProperty segments = editable.FindProperty("segments");
            int seen = 0;

            for (int s = 0; s < segments.arraySize; s++)
            {
                int count = segments.GetArrayElementAtIndex(s).FindPropertyRelative("lines").arraySize;
                if (nodeIndex < seen + count) return (s, nodeIndex - seen);
                seen += count;
            }
            return (-1, -1);
        }

        private void Select(DialogueScript wanted)
        {
            script = wanted;
            editable = wanted != null ? new SerializedObject(wanted) : null;
            segment = line = -1;
        }

        private bool Matches(DialogueScript candidate) =>
            string.IsNullOrEmpty(filter) ||
            candidate.ScriptId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private static IEnumerable<DialogueScript> All() =>
            AssetDatabase.FindAssets("t:DialogueScript")
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(found => found != null && !string.IsNullOrEmpty(found.ScriptId))
                .OrderBy(found => found.ScriptId, StringComparer.OrdinalIgnoreCase);
    }
}
