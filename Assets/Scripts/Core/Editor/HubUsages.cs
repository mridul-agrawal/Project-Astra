using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Editor
{
    // One place a name is used.
    public readonly struct HubUsage
    {
        public readonly UnityEngine.Object In;
        public readonly string Field;
        public readonly string Path;

        public HubUsage(UnityEngine.Object owner, string field, string path)
        {
            In = owner;
            Field = field;
            Path = path;
        }

        public string Reads => $"{(In != null ? In.name : "something")}  ·  {Field}";
    }

    // Everywhere a name is used, found by reading every field of every piece of hub content rather
    // than by a list somebody has to remember to extend.
    //
    // Nothing here knows what a conversation id means or where one is allowed to appear. That is
    // deliberate: a new field holding a name is found the day it is added.
    public static class HubUsages
    {
        private static readonly Type[] Authored =
        {
            typeof(HubLocationData), typeof(HubVisitData), typeof(HubEventData),
            typeof(QuestData), typeof(QuestObjective), typeof(DialogueScript),
            typeof(HubLocationDatabase), typeof(HubVisitDatabase), typeof(HubEventDatabase),
            typeof(QuestCatalog), typeof(DialogueScriptCatalog)
        };

        private static readonly Dictionary<string, List<HubUsage>> Remembered = new();

        public static IReadOnlyList<HubUsage> Of(string id)
        {
            if (string.IsNullOrEmpty(id)) return Array.Empty<HubUsage>();
            if (Remembered.TryGetValue(id, out List<HubUsage> known)) return known;

            return Remembered[id] = Look(id).ToList();
        }

        public static int CountOf(string id) => Of(id).Count;

        public static void Forget() => Remembered.Clear();

        private static IEnumerable<HubUsage> Look(string id) =>
            Everything().SelectMany(owner => In(owner, id));

        private static IEnumerable<UnityEngine.Object> Everything()
        {
            foreach (Type type in Authored)
                foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
                    if (asset != null) yield return asset;
                }

            // Doors and objects live in the room rather than in an asset, so whatever scene is open
            // is read too.
            foreach (InteractableBehaviour part in Resources.FindObjectsOfTypeAll<InteractableBehaviour>())
                if (part != null) yield return part;
        }

        private static IEnumerable<HubUsage> In(UnityEngine.Object owner, string id)
        {
            SerializedProperty field = new SerializedObject(owner).GetIterator();

            while (field.NextVisible(enterChildren: true))
            {
                if (field.propertyType != SerializedPropertyType.String) continue;
                if (field.stringValue != id) continue;

                yield return new HubUsage(owner, Readable(field.propertyPath), field.propertyPath);
            }
        }

        // "characterPlacements.Array.data[2].conversationId" is how Unity says it; a designer would
        // say "character placements 3, conversation id".
        private static string Readable(string path)
        {
            string tidied = Regex.Replace(path, @"\.Array\.data\[(\d+)\]", match =>
                $" {int.Parse(match.Groups[1].Value) + 1}");

            return string.Join(", ", tidied.Split('.').Select(ObjectNames.NicifyVariableName));
        }

        // The answers go stale when content changes, and the window asks on every repaint.
        private sealed class Watcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] deleted, string[] moved, string[] movedFrom) => Forget();
        }

        [InitializeOnLoadMethod]
        private static void WatchTheScene() => EditorApplication.hierarchyChanged += Forget;
    }
}
