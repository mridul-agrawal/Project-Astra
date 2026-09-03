using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // What names exist in the hub right now, so every id field can be a dropdown.
    //
    // Answers are cached: a property drawer asks on every repaint, and the answer only changes when
    // an asset or the scene does.
    public static class HubIds
    {
        private static readonly Dictionary<HubIdKind, string[]> Cached = new();

        // Gates and signals are named by design as they are needed; nothing declares them, so the
        // list is whatever is already being used and "New…" is how another one starts existing.
        public static bool IsNamedInPlace(HubIdKind kind) =>
            kind is HubIdKind.Gate or HubIdKind.Signal;

        public static string[] Of(HubIdKind kind)
        {
            if (Cached.TryGetValue(kind, out string[] known)) return known;
            return Cached[kind] = Gather(kind);
        }

        public static void Forget() => Cached.Clear();

        private static string[] Gather(HubIdKind kind) => Tidy(kind switch
        {
            HubIdKind.Conversation => Assets<DialogueScript>().Select(a => a.ScriptId),
            HubIdKind.Location => Assets<HubLocationData>().Select(a => a.LocationId),
            HubIdKind.Character => Assets<UnitDefinition>().Select(a => a.UnitId),
            HubIdKind.Event => Assets<HubEventData>().Select(a => a.EventId),
            HubIdKind.Quest => Assets<QuestData>().Select(a => a.QuestId),
            HubIdKind.Objective => Assets<QuestObjective>().Select(a => a.ObjectiveId),
            HubIdKind.Visit => Assets<HubVisitData>().Select(a => a.VisitId),
            HubIdKind.Map => Assets<MapData>().Select(a => a.MapId),
            HubIdKind.Interactable => InTheScene<InspectableInteractable>().Select(i => i.InteractableId),
            HubIdKind.Door => InTheScene<DoorInteractable>().Select(d => d.DoorId),
            HubIdKind.Gate => Gates(),
            HubIdKind.Signal => Signals(),
            _ => Enumerable.Empty<string>()
        });

        // Everywhere a gate is mentioned: opened by a visit, opened by an event or a quest stage,
        // or required by a door or an object.
        private static IEnumerable<string> Gates()
        {
            foreach (HubVisitData visit in Assets<HubVisitData>())
                foreach (string gate in visit.OpenGates)
                    yield return gate;

            foreach (HubEventAction action in EveryAction())
                if (action.kind == HubEventActionKind.SetGate)
                    yield return action.targetId;

            foreach (QuestEvent step in EveryQuestEvent())
                if (step is SetFlagEvent flag)
                    yield return flag.FlagId;

            foreach (DoorInteractable door in InTheScene<DoorInteractable>())
                yield return door.RequiredGate;

            foreach (InspectableInteractable thing in InTheScene<InspectableInteractable>())
                yield return thing.RequiredGate;
        }

        // Everywhere a signal is raised or waited on.
        private static IEnumerable<string> Signals()
        {
            foreach (HubEventAction action in EveryAction())
                if (action.kind == HubEventActionKind.RaiseFlag)
                    yield return action.valueId;

            foreach (QuestObjective objective in Assets<QuestObjective>())
                if (objective.Completion is SignalCondition signals)
                    foreach (string id in signals.Targets)
                        yield return id;
        }

        private static IEnumerable<HubEventAction> EveryAction() =>
            Assets<HubEventData>().SelectMany(e => e.Actions);

        private static IEnumerable<QuestEvent> EveryQuestEvent() =>
            Assets<QuestObjective>().SelectMany(o => o.OnStart.Concat(o.OnComplete));

        private static string[] Tidy(IEnumerable<string> ids) => ids
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .OrderBy(id => id, System.StringComparer.OrdinalIgnoreCase)
            .ToArray();

        private static IEnumerable<T> Assets<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null);

        // Doors and objects are authored in the room rather than in an asset, so these come from
        // whatever scene is open. Opening a scene to answer a dropdown would stall every repaint.
        private static IEnumerable<T> InTheScene<T>() where T : Object =>
            Resources.FindObjectsOfTypeAll<T>().Where(found => found != null);

        // The answers go stale when an asset is saved or something is added to the room.
        private sealed class Watcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] deleted, string[] moved, string[] movedFrom) => Forget();
        }

        [InitializeOnLoadMethod]
        private static void WatchTheScene() => EditorApplication.hierarchyChanged += Forget;
    }
}
