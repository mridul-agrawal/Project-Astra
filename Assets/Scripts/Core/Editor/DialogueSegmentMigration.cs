using System.Collections.Generic;
using ProjectAstra.Core.Dialogue;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // Lossless, one-time migration of dialogue scripts from the flat _nodes list to
    // the _segments structure. Consecutive nodes that share background + text speed +
    // auto-advance become one segment; each node becomes a line. The legacy _nodes
    // list is left intact and a *.backup.asset is written first, so nothing is lost.
    public static class DialogueSegmentMigration
    {
        const string AssetExt = ".asset";

        [MenuItem("Project Astra/Dialogue/Migrate Scripts to Segments")]
        public static void MigrateAll()
        {
            int migrated = 0;
            foreach (var path in DialogueScriptPaths())
                if (MigrateOne(path)) migrated++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueMigration] Migrated {migrated} script(s). Legacy _nodes kept; backups written.");
        }

        private static bool MigrateOne(string path)
        {
            var script = AssetDatabase.LoadAssetAtPath<DialogueScript>(path);
            if (script == null) return false;

            var so = new SerializedObject(script);
            var segments = so.FindProperty("_segments");
            var nodes = so.FindProperty("_nodes");

            if (segments.arraySize > 0) { Debug.Log($"[DialogueMigration] Skip (already segmented): {path}"); return false; }
            if (nodes.arraySize == 0) { Debug.Log($"[DialogueMigration] Skip (no legacy nodes): {path}"); return false; }

            BackupAsset(path);

            var groups = GroupNodes(nodes);
            WriteSegments(segments, groups);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(script);
            Debug.Log($"[DialogueMigration] {path}: {nodes.arraySize} nodes -> {groups.Count} segments (legacy kept).");
            return true;
        }

        // A new segment begins whenever the background, speed, or auto-advance changes.
        private static List<Group> GroupNodes(SerializedProperty nodes)
        {
            var groups = new List<Group>();
            Group current = null;
            for (int i = 0; i < nodes.arraySize; i++)
            {
                var n = nodes.GetArrayElementAtIndex(i);
                var bg = n.FindPropertyRelative("_fullScreenImage").objectReferenceValue;
                float speed = n.FindPropertyRelative("_textSpeedOverride").floatValue;
                float auto = n.FindPropertyRelative("_autoAdvanceDelay").floatValue;

                if (current == null || current.Background != bg
                    || !Mathf.Approximately(current.Speed, speed) || !Mathf.Approximately(current.Auto, auto))
                {
                    current = new Group { Background = bg, Speed = speed, Auto = auto };
                    groups.Add(current);
                }

                current.Lines.Add(new LineData
                {
                    Speaker = n.FindPropertyRelative("_speakerId").stringValue,
                    Expression = n.FindPropertyRelative("_expression").enumValueIndex,
                    Position = n.FindPropertyRelative("_portraitPosition").enumValueIndex,
                    Facing = n.FindPropertyRelative("_portraitFacing").enumValueIndex,
                    Text = n.FindPropertyRelative("_text").stringValue
                });
            }
            return groups;
        }

        private static void WriteSegments(SerializedProperty segments, List<Group> groups)
        {
            segments.arraySize = groups.Count;
            for (int g = 0; g < groups.Count; g++)
            {
                var seg = segments.GetArrayElementAtIndex(g);
                var group = groups[g];
                seg.FindPropertyRelative("_background").objectReferenceValue = group.Background;
                seg.FindPropertyRelative("_textSpeed").floatValue = group.Speed;
                seg.FindPropertyRelative("_autoAdvanceDelay").floatValue = group.Auto;

                var lines = seg.FindPropertyRelative("_lines");
                lines.arraySize = group.Lines.Count;
                for (int l = 0; l < group.Lines.Count; l++)
                {
                    var line = lines.GetArrayElementAtIndex(l);
                    var ld = group.Lines[l];
                    line.FindPropertyRelative("_speakerId").stringValue = ld.Speaker;
                    line.FindPropertyRelative("_expression").enumValueIndex = ld.Expression;
                    line.FindPropertyRelative("_portraitPosition").enumValueIndex = ld.Position;
                    line.FindPropertyRelative("_portraitFacing").enumValueIndex = ld.Facing;
                    line.FindPropertyRelative("_text").stringValue = ld.Text;
                }
            }
        }

        private static void BackupAsset(string path)
        {
            string backup = path.Substring(0, path.Length - AssetExt.Length) + ".backup.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(backup) == null)
                AssetDatabase.CopyAsset(path, backup);
        }

        [MenuItem("Project Astra/Dialogue/Clear Legacy Nodes (after verifying segments)")]
        public static void ClearLegacyNodes()
        {
            if (!EditorUtility.DisplayDialog("Clear legacy nodes?",
                "Removes the old _nodes list from every dialogue script that already has segments. " +
                "Do this only after confirming the segmented data is correct. The *.backup.asset files remain.",
                "Clear", "Cancel"))
                return;

            int cleared = 0;
            foreach (var path in DialogueScriptPaths())
            {
                var script = AssetDatabase.LoadAssetAtPath<DialogueScript>(path);
                if (script == null) continue;
                var so = new SerializedObject(script);
                if (so.FindProperty("_segments").arraySize == 0) continue;
                var nodes = so.FindProperty("_nodes");
                if (nodes.arraySize == 0) continue;
                nodes.arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(script);
                cleared++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DialogueMigration] Cleared legacy _nodes from {cleared} script(s).");
        }

        private static IEnumerable<string> DialogueScriptPaths()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(DialogueScript)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains(".backup")) yield return path; // never touch backup copies
            }
        }

        private struct LineData
        {
            public string Speaker;
            public int Expression, Position, Facing;
            public string Text;
        }

        private class Group
        {
            public Object Background;
            public float Speed, Auto;
            public readonly List<LineData> Lines = new();
        }
    }
}
