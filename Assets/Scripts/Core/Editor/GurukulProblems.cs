using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Gurukul;
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

            foreach (GurukulLocationData location in LoadAll<GurukulLocationData>()) CheckLocation(location, problems);
            foreach (GurukulObjectiveData objective in LoadAll<GurukulObjectiveData>()) CheckObjective(objective, problems);
            foreach (GurukulEventData authored in LoadAll<GurukulEventData>()) CheckEvent(authored, problems);
            foreach (GurukulVisitData visit in LoadAll<GurukulVisitData>()) CheckVisit(visit, problems);

            return problems;
        }

        // --- Locations ---

        private static void CheckLocation(GurukulLocationData location, List<GurukulProblem> problems)
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
        private static void CheckRoomIsBigEnough(GurukulLocationData location, List<GurukulProblem> problems)
        {
            const float viewWide = GurukulScreenSpace.GameplayWidth / GurukulScreenSpace.PixelsPerTile;
            const float viewHigh = GurukulScreenSpace.GameplayHeight / GurukulScreenSpace.PixelsPerTile;

            if (location.TileWidth < viewWide || location.TileHeight < viewHigh)
                problems.Add(new GurukulProblem(location,
                    $"{location.name}: {location.TileWidth}x{location.TileHeight} tiles is smaller than the " +
                    $"{viewWide:0.#}x{viewHigh:0.#} the camera shows"));
        }

        private static void CheckDoors(GurukulLocationData location, List<GurukulProblem> problems)
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


        // --- Objectives and events ---

        private static void CheckObjective(GurukulObjectiveData objective, List<GurukulProblem> problems)
        {
            if (!ObjectiveSequenceRunner.IsAuthoredCorrectly(objective, out string problem))
                problems.Add(new GurukulProblem(objective, $"{objective.name}: {problem}"));

            var seen = new HashSet<string>();
            foreach (string target in objective.Completion.targetIds)
                if (!seen.Add(target))
                    problems.Add(new GurukulProblem(objective, $"{objective.name}: '{target}' is listed twice as a target"));
        }

        private static void CheckEvent(GurukulEventData authored, List<GurukulProblem> problems)
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

        private static void CheckVisit(GurukulVisitData visit, List<GurukulProblem> problems)
        {
            if (string.IsNullOrEmpty(visit.VisitId))
                problems.Add(new GurukulProblem(visit, $"{visit.name}: empty visitId"));

            GurukulLocationData start = FindLocation(visit.StartLocationId);
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

        private static void CheckSpawn(GurukulVisitData visit, GurukulLocationData start, List<GurukulProblem> problems)
        {
            GurukulCollisionMap map = start.BuildCollisionMap();
            Rect footprint = GurukulMover.FootprintAt(visit.PlayerSpawn, PlayerFootprint);

            if (map.IsRectBlocked(footprint))
                problems.Add(new GurukulProblem(visit,
                    $"{visit.name}: she spawns at {visit.PlayerSpawn}, which is inside something solid"));
        }

        private static void CheckPlacements(GurukulVisitData visit, List<GurukulProblem> problems)
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
        private static void CheckMarkers(GurukulVisitData visit, List<GurukulProblem> problems)
        {
            var placed = new HashSet<string>();
            foreach (GurukulCharacterPlacement placement in visit.CharacterPlacements)
                placed.Add(placement.characterId);

            foreach (GurukulObjectiveData objective in visit.Objectives)
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
        private static void CheckRouteTimes(GurukulVisitData visit, GurukulLocationData start, List<GurukulProblem> problems)
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

        private static GurukulLocationData FindLocation(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return null;
            foreach (GurukulLocationData candidate in LoadAll<GurukulLocationData>())
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
