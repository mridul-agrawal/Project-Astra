using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Shows everything wrong with the authored hub content, with a click through to the asset that
    // owns each one. The spec calls these blocking content errors; this is where they surface.
    public class HubProblemsWindow : EditorWindow
    {
        private List<HubProblem> problems;
        private Vector2 scroll;

        [MenuItem("Project Astra/Hub/Content Problems")]
        public static void Open()
        {
            var window = GetWindow<HubProblemsWindow>("Hub Problems");
            window.minSize = new Vector2(560, 320);
            window.Refresh();
        }

        // The same check written to the console, for running it without opening a window.
        [MenuItem("Project Astra/Hub/Log Content Problems")]
        public static void LogProblems()
        {
            List<HubProblem> found = HubProblems.Collect();
            if (found.Count == 0)
            {
                Debug.Log("[HubProblems] No problems found.");
                return;
            }

            var report = new System.Text.StringBuilder($"[HubProblems] {found.Count} problem(s):");
            foreach (HubProblem problem in found) report.Append("\n  ").Append(problem.Message);
            Debug.LogWarning(report.ToString());
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Check again")) Refresh();

            if (problems == null)
            {
                EditorGUILayout.HelpBox("Nothing checked yet.", MessageType.Info);
                return;
            }

            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{problems.Count} problem(s)", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (HubProblem problem in problems) DrawProblem(problem);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawProblem(HubProblem problem)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(problem.Message, EditorStyles.wordWrappedLabel);

            using (new EditorGUI.DisabledScope(problem.Asset == null))
                if (GUILayout.Button("Show", GUILayout.Width(52)))
                    Selection.activeObject = problem.Asset;

            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            problems = HubProblems.Collect();
            Repaint();
        }
    }
}
