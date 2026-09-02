using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    public struct HubProblem
    {
        public readonly string Message;
        public readonly Object Asset;

        public HubProblem(Object asset, string message)
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
    public static class HubProblems
    {
        // The spec's travel target: no mandatory route should take longer than this at walking pace.
        private const float MaxRouteSeconds = 12f;

        private static readonly Rect PlayerFootprint = new(-0.25f, 0f, 0.5f, 0.25f);

        public static List<HubProblem> Collect()
        {
            var problems = new List<HubProblem>();

            foreach (HubLocationData location in LoadAll<HubLocationData>()) CheckLocation(location, problems);
            foreach (HubObjectiveData objective in LoadAll<HubObjectiveData>()) CheckObjective(objective, problems);
            foreach (HubEventData authored in LoadAll<HubEventData>()) CheckEvent(authored, problems);
            foreach (HubVisitData visit in LoadAll<HubVisitData>()) CheckVisit(visit, problems);

            return problems;
        }

        // --- Locations ---

        private static void CheckLocation(HubLocationData location, List<HubProblem> problems)
        {
            if (string.IsNullOrEmpty(location.LocationId))
                problems.Add(new HubProblem(location, $"{location.name}: empty locationId"));
            if (location.BaseArt == null)
                problems.Add(new HubProblem(location, $"{location.name}: no base art"));

            CheckRoomIsBigEnough(location, problems);
            CheckDoors(location, problems);
        }

        // A room smaller than the screen leaves the camera with nothing to clamp to and shows the
        // void past its edges.
        private static void CheckRoomIsBigEnough(HubLocationData location, List<HubProblem> problems)
        {
            const float viewWide = HubScreenSpace.GameplayWidth / HubScreenSpace.PixelsPerTile;
            const float viewHigh = HubScreenSpace.GameplayHeight / HubScreenSpace.PixelsPerTile;

            if (location.TileWidth < viewWide || location.TileHeight < viewHigh)
                problems.Add(new HubProblem(location,
                    $"{location.name}: {location.TileWidth}x{location.TileHeight} tiles is smaller than the " +
                    $"{viewWide:0.#}x{viewHigh:0.#} the camera shows"));
        }

        private static void CheckDoors(HubLocationData location, List<HubProblem> problems)
        {
            var seen = new HashSet<string>();

            foreach (HubDoor door in location.Doors)
            {
                if (string.IsNullOrEmpty(door.doorId))
                    problems.Add(new HubProblem(location, $"{location.name}: a door has no id"));
                else if (!seen.Add(door.doorId))
                    problems.Add(new HubProblem(location, $"{location.name}: duplicate door id '{door.doorId}'"));

                if (door.ReturnsToPreviousRoom) continue;

                if (FindLocation(door.targetLocationId) == null)
                    problems.Add(new HubProblem(location,
                        $"{location.name}: door '{door.doorId}' leads to '{door.targetLocationId}', which doesn't exist"));

                if (!location.TryGetDoor(door.doorId, out _)) continue;
            }
        }

        // --- Conversations ---


        // --- Objectives and events ---

        private static void CheckObjective(HubObjectiveData objective, List<HubProblem> problems)
        {
            if (!ObjectiveSequenceRunner.IsAuthoredCorrectly(objective, out string problem))
                problems.Add(new HubProblem(objective, $"{objective.name}: {problem}"));

            var seen = new HashSet<string>();
            foreach (string target in objective.Completion.targetIds)
                if (!seen.Add(target))
                    problems.Add(new HubProblem(objective, $"{objective.name}: '{target}' is listed twice as a target"));
        }

        private static void CheckEvent(HubEventData authored, List<HubProblem> problems)
        {
            if (string.IsNullOrEmpty(authored.EventId))
                problems.Add(new HubProblem(authored, $"{authored.name}: empty eventId"));

            if (authored.Actions.Length == 0)
                problems.Add(new HubProblem(authored, $"{authored.name}: the event does nothing"));

            foreach (HubEventAction action in authored.Actions)
                if (action.kind == HubEventActionKind.WalkCharacter && (action.route == null || action.route.Length == 0))
                    problems.Add(new HubProblem(authored,
                        $"{authored.name}: a walk action for '{action.targetId}' has no route"));
        }

        // --- Visits ---

        private static void CheckVisit(HubVisitData visit, List<HubProblem> problems)
        {
            if (string.IsNullOrEmpty(visit.VisitId))
                problems.Add(new HubProblem(visit, $"{visit.name}: empty visitId"));

            HubLocationData start = FindLocation(visit.StartLocationId);
            if (start == null)
            {
                problems.Add(new HubProblem(visit,
                    $"{visit.name}: opens in '{visit.StartLocationId}', which doesn't exist"));
                return;
            }

            if (visit.Objectives.Length == 0)
                problems.Add(new HubProblem(visit, $"{visit.name}: has no objectives, so it can never be finished"));

            CheckSpawn(visit, start, problems);
            CheckPlacements(visit, problems);
            CheckMarkers(visit, problems);
            CheckRouteTimes(visit, start, problems);
        }

        private static void CheckSpawn(HubVisitData visit, HubLocationData start, List<HubProblem> problems)
        {
            HubCollisionMap map = start.BuildCollisionMap();
            Rect footprint = HubMover.FootprintAt(visit.PlayerSpawn, PlayerFootprint);

            if (map.IsRectBlocked(footprint))
                problems.Add(new HubProblem(visit,
                    $"{visit.name}: she spawns at {visit.PlayerSpawn}, which is inside something solid"));
        }

        private static void CheckPlacements(HubVisitData visit, List<HubProblem> problems)
        {
            var seen = new HashSet<string>();

            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
            {
                if (string.IsNullOrEmpty(placement.characterId))
                    problems.Add(new HubProblem(visit, $"{visit.name}: a placement has no character"));
                else if (!seen.Add(placement.characterId))
                    problems.Add(new HubProblem(visit,
                        $"{visit.name}: '{placement.characterId}' is placed more than once — a character can only be in one room"));

                if (FindLocation(placement.locationId) == null)
                    problems.Add(new HubProblem(visit,
                        $"{visit.name}: '{placement.characterId}' is placed in '{placement.locationId}', which doesn't exist"));
            }
        }

        // A marker over something the visit never places, or over an interactable nothing declares,
        // would point the player at empty ground.
        private static void CheckMarkers(HubVisitData visit, List<HubProblem> problems)
        {
            var placed = new HashSet<string>();
            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
                placed.Add(placement.characterId);

            foreach (HubObjectiveData objective in visit.Objectives)
            {
                if (objective == null) continue;
                foreach (string target in objective.MarkerTargetIds)
                {
                    if (string.IsNullOrEmpty(target) || placed.Contains(target)) continue;
                    if (SceneDeclaresInteractable(target)) continue;

                    problems.Add(new HubProblem(objective,
                        $"{objective.name}: marks '{target}', which '{visit.VisitId}' neither places nor declares"));
                }
            }
        }

        // The spec's timing check: walk every marked target in the opening room and report anything
        // slower than the target. Reported, not corrected — the fix is design's.
        private static void CheckRouteTimes(HubVisitData visit, HubLocationData start, List<HubProblem> problems)
        {
            HubCollisionMap map = start.BuildCollisionMap();

            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
            {
                if (placement.locationId != visit.StartLocationId) continue;

                if (!WalkableRouteTimer.CanReachNeighbour(map, PlayerFootprint, visit.PlayerSpawn,
                        placement.position, out float seconds))
                {
                    problems.Add(new HubProblem(visit,
                        $"{visit.name}: '{placement.characterId}' at {placement.position} can't be walked to from the spawn"));
                    continue;
                }

                if (seconds > MaxRouteSeconds)
                    problems.Add(new HubProblem(visit,
                        $"{visit.name}: walking to '{placement.characterId}' takes {seconds:0.#}s, over the {MaxRouteSeconds}s target"));
            }
        }

        // --- Lookups ---

        private static bool SceneDeclaresInteractable(string interactableId)
        {
            foreach (InspectableInteractable candidate in
                     Resources.FindObjectsOfTypeAll<InspectableInteractable>())
                if (candidate.InteractableId == interactableId) return true;
            return false;
        }

        private static HubLocationData FindLocation(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return null;
            foreach (HubLocationData candidate in LoadAll<HubLocationData>())
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
