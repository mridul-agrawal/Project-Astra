using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;
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

    // One pass over every authored hub asset, surfacing the mistakes the inspector can't show.
    public static class HubProblems
    {
        // The spec's travel target: no mandatory route should take longer than this at walking pace.
        private const float MaxRouteSeconds = 12f;

        private static readonly Rect PlayerFootprint = new(-0.25f, 0f, 0.5f, 0.25f);

        public static List<HubProblem> Collect()
        {
            var problems = new List<HubProblem>();

            foreach (HubLocationData location in LoadAll<HubLocationData>()) CheckLocation(location, problems);
            foreach (QuestObjective objective in LoadAll<QuestObjective>()) CheckObjective(objective, problems);
            foreach (HubEventData authored in LoadAll<HubEventData>()) CheckEvent(authored, problems);
            foreach (HubVisitData visit in LoadAll<HubVisitData>()) CheckVisit(visit, problems);

            return problems;
        }

        // --- Locations ---

        private static void CheckLocation(HubLocationData location, List<HubProblem> problems)
        {
            if (string.IsNullOrEmpty(location.LocationId))
                problems.Add(new HubProblem(location, $"{location.name}: empty locationId"));
            if (location.RoomPrefab == null)
                problems.Add(new HubProblem(location, $"{location.name}: no room prefab, so it cannot be shown"));

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

        // Doors live in the room now, so they are read off the prefab rather than a list.
        private static void CheckDoors(HubLocationData location, List<HubProblem> problems)
        {
            if (location.RoomPrefab == null) return;

            var seen = new HashSet<string>();
            foreach (DoorInteractable door in location.RoomPrefab.GetComponentsInChildren<DoorInteractable>(true))
            {
                if (string.IsNullOrEmpty(door.DoorId))
                    problems.Add(new HubProblem(location, $"{location.name}: a door has no id"));
                else if (!seen.Add(door.DoorId))
                    problems.Add(new HubProblem(location, $"{location.name}: duplicate door id '{door.DoorId}'"));

                if (door.ReturnsToPreviousRoom) continue;

                if (FindLocation(door.TargetLocationId) == null)
                    problems.Add(new HubProblem(location,
                        $"{location.name}: door '{door.DoorId}' leads to '{door.TargetLocationId}', which doesn't exist"));
            }
        }

        // --- Conversations ---


        // --- Objectives and events ---

        private static void CheckObjective(QuestObjective objective, List<HubProblem> problems)
        {
            if (string.IsNullOrEmpty(objective.ObjectiveId))
                problems.Add(new HubProblem(objective, $"{objective.name}: empty objectiveId"));
            if (string.IsNullOrEmpty(objective.DisplayText))
                problems.Add(new HubProblem(objective, $"{objective.name}: no player-facing text"));

            if (objective.Completion == null)
            {
                problems.Add(new HubProblem(objective, $"{objective.name}: no completion condition"));
                return;
            }
            if (!objective.Completion.IsAuthoredCorrectly(out string problem))
                problems.Add(new HubProblem(objective, $"{objective.name}: {problem}"));

            var seen = new HashSet<string>();
            foreach (string target in objective.Completion.Targets)
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

            QuestData quest = FindQuest(visit.QuestId);
            if (quest == null)
                problems.Add(new HubProblem(visit, $"{visit.name}: names quest '{visit.QuestId}', which doesn't exist"));
            else if (quest.Objectives.Length == 0)
                problems.Add(new HubProblem(visit, $"{visit.name}: quest '{quest.QuestId}' has no objectives, so it can never be finished"));

            CheckSpawn(visit, start, problems);
            CheckPlacements(visit, problems);
            CheckMarkers(visit, problems);
            CheckRouteTimes(visit, start, problems);
        }

        private static void CheckSpawn(HubVisitData visit, HubLocationData start, List<HubProblem> problems)
        {
            using var room = new RoomUnderTest(start);
            if (room.Space == null) return;

            if (room.Space.IsBlocked(HubMover.FootprintAt(visit.PlayerSpawn, PlayerFootprint)))
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

            foreach (QuestObjective objective in ObjectivesOf(visit))
            {
                if (objective == null) continue;
                foreach (string target in MarkersOf(objective))
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
            using var room = new RoomUnderTest(start);
            if (room.Space == null) return;

            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
            {
                if (placement.locationId != visit.StartLocationId) continue;

                if (!WalkableRouteTimer.CanReachNeighbour(room.Space, PlayerFootprint, visit.PlayerSpawn,
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

        // A visit names a quest; the quest owns the stages. An unknown id is its own problem,
        // reported where the visit is checked.
        private static IEnumerable<QuestObjective> ObjectivesOf(HubVisitData visit)
        {
            QuestData quest = FindQuest(visit.QuestId);
            return quest != null ? quest.Objectives : System.Array.Empty<QuestObjective>();
        }

        // Markers are the condition's outstanding targets unless the objective overrides them.
        private static IEnumerable<string> MarkersOf(QuestObjective objective)
        {
            if (objective.MarkerTargetIds.Length > 0) return objective.MarkerTargetIds;
            if (objective.Completion == null) return System.Array.Empty<string>();

            var anchors = new List<string>();
            foreach (string target in objective.Completion.Targets)
            {
                string anchor = objective.Completion.MarkerFor(target);
                if (!string.IsNullOrEmpty(anchor)) anchors.Add(anchor);
            }
            return anchors;
        }

        private static QuestData FindQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            foreach (QuestData candidate in LoadAll<QuestData>())
                if (candidate.QuestId == questId) return candidate;
            return null;
        }

        // The room's colliders, stood up in a scene of their own so a content check never disturbs
        // whatever the designer happens to have open, and never sees its colliders either.
        private sealed class RoomUnderTest : System.IDisposable
        {
            private readonly UnityEngine.SceneManagement.Scene scene;
            private readonly bool opened;

            public ISolidSpace Space { get; }

            public RoomUnderTest(HubLocationData location)
            {
                if (location == null || location.RoomPrefab == null) return;

                scene = EditorSceneManager.NewPreviewScene();
                opened = true;

                GameObject room = Object.Instantiate(location.RoomPrefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
                Physics2D.SyncTransforms();

                Space = new PreviewSolidSpace(scene, location.Bounds);
            }

            public void Dispose()
            {
                if (opened) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // The same question PhysicsSolidSpace answers, asked of one preview scene's own physics.
        private sealed class PreviewSolidSpace : ISolidSpace
        {
            private static readonly Collider2D[] Hit = new Collider2D[1];

            private readonly PhysicsScene2D physics;
            private readonly Rect bounds;
            private readonly ContactFilter2D filter;

            public PreviewSolidSpace(UnityEngine.SceneManagement.Scene scene, Rect bounds)
            {
                physics = scene.GetPhysicsScene2D();
                this.bounds = bounds;
                filter = new ContactFilter2D
                {
                    useTriggers = false,
                    useLayerMask = true,
                    layerMask = LayerMask.GetMask(PhysicsSolidSpace.SolidLayer)
                };
            }

            public bool IsBlocked(Rect footprint) =>
                !Inside(footprint) ||
                physics.OverlapBox(footprint.center, footprint.size, 0f, filter, Hit) > 0;

            private bool Inside(Rect footprint) =>
                footprint.xMin >= bounds.xMin && footprint.xMax <= bounds.xMax &&
                footprint.yMin >= bounds.yMin && footprint.yMax <= bounds.yMax;
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
