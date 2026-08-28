using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Gurukul.Conversation;
using ProjectAstra.Core.Gurukul.Events;

namespace ProjectAstra.Core.Editor
{
    public struct GurukulProblem
    {
        public readonly string Message;
        public readonly Object Asset;

        public GurukulProblem(Object asset, string message)
        {
            Asset = asset;
            Message = message;
        }
    }

    // One pass over every authored hub asset, surfacing the things the inspector can't: an objective
    // nobody can finish, a marker over something that isn't there, a door with no way back, a
    // conversation branch that leads nowhere.
    //
    // The spec calls these blocking content errors and expects them found before play rather than
    // during it, so this is the checklist in code. Same shape as DataHubProblems, and shown the same
    // way.
    public static class GurukulProblems
    {
        // The spec's travel target: no mandatory route should take longer than this at walking pace.
        private const float MaxRouteSeconds = 12f;

        private static readonly Rect PlayerFootprint = new(-0.25f, 0f, 0.5f, 0.25f);

        public static List<GurukulProblem> Collect()
        {
            var problems = new List<GurukulProblem>();

            foreach (GurukulLocation location in LoadAll<GurukulLocation>()) CheckLocation(location, problems);
            foreach (ConversationGraph graph in LoadAll<ConversationGraph>()) CheckConversation(graph, problems);
            foreach (GurukulObjective objective in LoadAll<GurukulObjective>()) CheckObjective(objective, problems);
            foreach (GurukulEvent authored in LoadAll<GurukulEvent>()) CheckEvent(authored, problems);
            foreach (GurukulVisit visit in LoadAll<GurukulVisit>()) CheckVisit(visit, problems);

            return problems;
        }

        // --- Locations ---

        private static void CheckLocation(GurukulLocation location, List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(location.LocationId))
                problems.Add(new GurukulProblem(location, $"{location.name}: empty locationId"));
            if (location.BaseArt == null)
                problems.Add(new GurukulProblem(location, $"{location.name}: no base art"));

            CheckRoomIsBigEnough(location, problems);
            CheckDoors(location, problems);
        }

        // A room smaller than the screen leaves the camera with nothing to clamp to and shows the
        // void past its edges.
        private static void CheckRoomIsBigEnough(GurukulLocation location, List<GurukulProblem> problems)
        {
            const float viewWide = GurukulScreenSpace.GameplayWidth / GurukulScreenSpace.PixelsPerTile;
            const float viewHigh = GurukulScreenSpace.GameplayHeight / GurukulScreenSpace.PixelsPerTile;

            if (location.TileWidth < viewWide || location.TileHeight < viewHigh)
                problems.Add(new GurukulProblem(location,
                    $"{location.name}: {location.TileWidth}x{location.TileHeight} tiles is smaller than the " +
                    $"{viewWide:0.#}x{viewHigh:0.#} the camera shows"));
        }

        private static void CheckDoors(GurukulLocation location, List<GurukulProblem> problems)
        {
            var seen = new HashSet<string>();

            foreach (GurukulDoor door in location.Doors)
            {
                if (string.IsNullOrEmpty(door.doorId))
                    problems.Add(new GurukulProblem(location, $"{location.name}: a door has no id"));
                else if (!seen.Add(door.doorId))
                    problems.Add(new GurukulProblem(location, $"{location.name}: duplicate door id '{door.doorId}'"));

                if (door.ReturnsToPreviousRoom) continue;

                if (FindLocation(door.targetLocationId) == null)
                    problems.Add(new GurukulProblem(location,
                        $"{location.name}: door '{door.doorId}' leads to '{door.targetLocationId}', which doesn't exist"));

                if (!location.TryGetDoor(door.doorId, out _)) continue;
            }
        }

        // --- Conversations ---

        private static void CheckConversation(ConversationGraph graph, List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(graph.ConversationId))
                problems.Add(new GurukulProblem(graph, $"{graph.name}: empty conversationId"));

            if (graph.Find(graph.EntryNodeId) == null)
                problems.Add(new GurukulProblem(graph, $"{graph.name}: entry node '{graph.EntryNodeId}' doesn't exist"));

            if (!string.IsNullOrEmpty(graph.RepeatEntryNodeId) && graph.Find(graph.RepeatEntryNodeId) == null)
                problems.Add(new GurukulProblem(graph, $"{graph.name}: repeat entry '{graph.RepeatEntryNodeId}' doesn't exist"));

            foreach (ConversationNode node in graph.Nodes) CheckNode(graph, node, problems);
        }

        private static void CheckNode(ConversationGraph graph, ConversationNode node, List<GurukulProblem> problems)
        {
            if (node.kind == ConversationNodeKind.Script && node.script == null)
                problems.Add(new GurukulProblem(graph, $"{graph.name}: node '{node.nodeId}' plays no script"));

            bool offersOptions = node.kind is ConversationNodeKind.Choice or ConversationNodeKind.TopicMenu;
            if (offersOptions && (node.options == null || node.options.Length == 0))
                problems.Add(new GurukulProblem(graph, $"{graph.name}: node '{node.nodeId}' offers nothing to pick"));

            CheckLink(graph, node.nodeId, node.nextNodeId, problems);
            if (!offersOptions) return;

            CheckLink(graph, node.nodeId, node.cancelNodeId, problems);
            foreach (ConversationOption option in node.options)
            {
                if (string.IsNullOrEmpty(option.optionId))
                    problems.Add(new GurukulProblem(graph, $"{graph.name}: an option on '{node.nodeId}' has no id"));

                CheckLink(graph, node.nodeId, option.nextNodeId, problems);
                CheckLink(graph, node.nodeId, option.repeatNodeId, problems);
            }
        }

        // An empty link ends the conversation, which is fine. A link to a node that isn't there is
        // a dead end the player would fall through.
        private static void CheckLink(ConversationGraph graph, string fromNode, string toNode,
            List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(toNode) || graph.Find(toNode) != null) return;
            problems.Add(new GurukulProblem(graph, $"{graph.name}: '{fromNode}' points at '{toNode}', which doesn't exist"));
        }

        // --- Objectives and events ---

        private static void CheckObjective(GurukulObjective objective, List<GurukulProblem> problems)
        {
            if (!ObjectiveSequenceRunner.IsAuthoredCorrectly(objective, out string problem))
                problems.Add(new GurukulProblem(objective, $"{objective.name}: {problem}"));

            var seen = new HashSet<string>();
            foreach (string target in objective.Completion.targetIds)
                if (!seen.Add(target))
                    problems.Add(new GurukulProblem(objective, $"{objective.name}: '{target}' is listed twice as a target"));
        }

        private static void CheckEvent(GurukulEvent authored, List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(authored.EventId))
                problems.Add(new GurukulProblem(authored, $"{authored.name}: empty eventId"));

            if (authored.Actions.Length == 0)
                problems.Add(new GurukulProblem(authored, $"{authored.name}: the event does nothing"));

            foreach (GurukulEventAction action in authored.Actions)
                if (action.kind == GurukulEventActionKind.WalkCharacter && (action.route == null || action.route.Length == 0))
                    problems.Add(new GurukulProblem(authored,
                        $"{authored.name}: a walk action for '{action.targetId}' has no route"));
        }

        // --- Visits ---

        private static void CheckVisit(GurukulVisit visit, List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(visit.VisitId))
                problems.Add(new GurukulProblem(visit, $"{visit.name}: empty visitId"));

            GurukulLocation start = FindLocation(visit.StartLocationId);
            if (start == null)
            {
                problems.Add(new GurukulProblem(visit,
                    $"{visit.name}: opens in '{visit.StartLocationId}', which doesn't exist"));
                return;
            }

            if (visit.Objectives.Length == 0)
                problems.Add(new GurukulProblem(visit, $"{visit.name}: has no objectives, so it can never be finished"));

            CheckSpawn(visit, start, problems);
            CheckPlacements(visit, problems);
            CheckMarkers(visit, problems);
            CheckRouteTimes(visit, start, problems);
        }

        private static void CheckSpawn(GurukulVisit visit, GurukulLocation start, List<GurukulProblem> problems)
        {
            GurukulCollisionMap map = start.BuildCollisionMap();
            Rect footprint = GurukulMover.FootprintAt(visit.PlayerSpawn, PlayerFootprint);

            if (map.IsRectBlocked(footprint))
                problems.Add(new GurukulProblem(visit,
                    $"{visit.name}: she spawns at {visit.PlayerSpawn}, which is inside something solid"));
        }

        private static void CheckPlacements(GurukulVisit visit, List<GurukulProblem> problems)
        {
            var seen = new HashSet<string>();

            foreach (GurukulCharacterPlacement placement in visit.CharacterPlacements)
            {
                if (string.IsNullOrEmpty(placement.characterId))
                    problems.Add(new GurukulProblem(visit, $"{visit.name}: a placement has no character"));
                else if (!seen.Add(placement.characterId))
                    problems.Add(new GurukulProblem(visit,
                        $"{visit.name}: '{placement.characterId}' is placed more than once — a character can only be in one room"));

                if (FindLocation(placement.locationId) == null)
                    problems.Add(new GurukulProblem(visit,
                        $"{visit.name}: '{placement.characterId}' is placed in '{placement.locationId}', which doesn't exist"));
            }
        }

        // A marker over something the visit never places, or over an interactable nothing declares,
        // would point the player at empty ground.
        private static void CheckMarkers(GurukulVisit visit, List<GurukulProblem> problems)
        {
            var placed = new HashSet<string>();
            foreach (GurukulCharacterPlacement placement in visit.CharacterPlacements)
                placed.Add(placement.characterId);

            foreach (GurukulObjective objective in visit.Objectives)
            {
                if (objective == null) continue;
                foreach (string target in objective.MarkerTargetIds)
                {
                    if (string.IsNullOrEmpty(target) || placed.Contains(target)) continue;
                    if (SceneDeclaresInteractable(target)) continue;

                    problems.Add(new GurukulProblem(objective,
                        $"{objective.name}: marks '{target}', which '{visit.VisitId}' neither places nor declares"));
                }
            }
        }

        // The spec's timing check: walk every marked target in the opening room and report anything
        // slower than the target. Reported, not corrected — the fix is design's.
        private static void CheckRouteTimes(GurukulVisit visit, GurukulLocation start, List<GurukulProblem> problems)
        {
            GurukulCollisionMap map = start.BuildCollisionMap();

            foreach (GurukulCharacterPlacement placement in visit.CharacterPlacements)
            {
                if (placement.locationId != visit.StartLocationId) continue;

                if (!WalkableRouteTimer.CanReachNeighbour(map, PlayerFootprint, visit.PlayerSpawn,
                        placement.position, out float seconds))
                {
                    problems.Add(new GurukulProblem(visit,
                        $"{visit.name}: '{placement.characterId}' at {placement.position} can't be walked to from the spawn"));
                    continue;
                }

                if (seconds > MaxRouteSeconds)
                    problems.Add(new GurukulProblem(visit,
                        $"{visit.name}: walking to '{placement.characterId}' takes {seconds:0.#}s, over the {MaxRouteSeconds}s target"));
            }
        }

        // --- Lookups ---

        private static bool SceneDeclaresInteractable(string interactableId)
        {
            foreach (GurukulInteractable candidate in
                     Resources.FindObjectsOfTypeAll<GurukulInteractable>())
                if (candidate.InteractableId == interactableId) return true;
            return false;
        }

        private static GurukulLocation FindLocation(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return null;
            foreach (GurukulLocation candidate in LoadAll<GurukulLocation>())
                if (candidate.LocationId == locationId) return candidate;
            return null;
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            var found = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) found.Add(asset);
            }
            return found;
        }
    }
}
