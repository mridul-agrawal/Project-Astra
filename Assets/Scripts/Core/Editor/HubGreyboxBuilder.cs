using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    // Makes the programmer-art hub to build against: a courtyard, walls, a tree, and a house.
    public static class HubGreyboxBuilder
    {
        private const int TilePixels = 32;

        // Twice the 15-tile view, so the camera has to scroll.
        private const int CourtyardWide = 30;
        private const int CourtyardHigh = 18;
        // At least as big as the 15 x 8.4 tiles the camera shows, or it would reveal the void
        // past the room's edges.
        private const int HouseWide = 16;
        private const int HouseHigh = 10;

        private const string Folder = "Assets/Gurukul";
        private const string ArtFolder = Folder + "/Greybox";

        // The sprite pivots bottom-left, so this shifts the interaction point to the middle of its base.
        private static readonly Vector2 TreeFootOffset = new(0.5f, 0f);
        private const string DataFolder = Folder + "/Data";
        private const string PropsPrefabPath = ArtFolder + "/GreyboxProps.prefab";

        private static readonly Color Grass = new(0.36f, 0.44f, 0.28f);
        private static readonly Color GrassLine = new(0.32f, 0.40f, 0.25f);
        private static readonly Color Floor = new(0.42f, 0.34f, 0.26f);
        private static readonly Color FloorLine = new(0.38f, 0.31f, 0.23f);
        private static readonly Color Stone = new(0.30f, 0.27f, 0.24f);
        private static readonly Color TreeTrunk = new(0.28f, 0.20f, 0.14f);
        private static readonly Color TreeCanopy = new(0.20f, 0.34f, 0.20f);

        private static readonly Vector2 TreeFootPosition = new(8f, 6f);

        // Walls in tile coordinates, measured from the bottom-left.
        private static IEnumerable<RectInt> CourtyardWalls()
        {
            foreach (RectInt edge in Border(CourtyardWide, CourtyardHigh)) yield return edge;

            // A wall across the middle with a one-tile doorway, to prove she fits through a gap her
            // own width and can't squeeze past a corner.
            yield return new RectInt(14, 2, 1, 6);
            yield return new RectInt(14, 9, 1, 7);

            yield return new RectInt(4, 12, 5, 1);       // something to walk along
            yield return new RectInt(22, 4, 3, 3);       // a block to walk around
        }

        private static IEnumerable<RectInt> HouseWalls() => Border(HouseWide, HouseHigh);

        private static IEnumerable<RectInt> Border(int wide, int high)
        {
            yield return new RectInt(0, 0, wide, 1);
            yield return new RectInt(0, high - 1, wide, 1);
            yield return new RectInt(0, 0, 1, high);
            yield return new RectInt(wide - 1, 0, 1, high);
        }

        // Rebuilds only the greybox's data: the visit, its quest and its events. The rooms
        // themselves are authored prefabs under Assets/Gurukul/Rooms and are never touched here.
        [MenuItem("Project Astra/Hub/Build Greybox Data")]
        public static void Build()
        {
            HubLocationData courtyard = Find("Location_GreyboxCourtyard");
            if (courtyard == null)
            {
                Debug.LogError("[HubGreybox] No Location_GreyboxCourtyard to build a visit against.");
                return;
            }

            BuildEvents();
            BuildVisit(courtyard);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HubGreybox] Rebuilt the greybox visit, quest and events.");
        }

        private static HubLocationData Find(string assetName) =>
            AssetDatabase.LoadAssetAtPath<HubLocationData>($"{DataFolder}/{assetName}.asset");

        // --- Visit ---

        private static HubVisitData BuildVisit(HubLocationData location)
        {
            var visit = LoadOrCreate<HubVisitData>($"{DataFolder}/Visit_Greybox.asset");
            var serialized = new SerializedObject(visit);

            serialized.FindProperty("visitId").stringValue = "greybox";
            serialized.FindProperty("displayName").stringValue = "Greybox";
            serialized.FindProperty("startLocationId").stringValue = location.LocationId;
            serialized.FindProperty("playerSpawn").vector2Value = new Vector2(4f, 4f);
            serialized.FindProperty("playerFacing").enumValueIndex = (int)Facing.South;
            serialized.FindProperty("openingEventId").stringValue = "greybox_opening";
            WriteTestCharacter(serialized.FindProperty("characterPlacements"), location.LocationId);
            serialized.FindProperty("questId").stringValue = BuildQuest().QuestId;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(visit);
            return visit;
        }

        // One character to walk up to. Uses an existing unit so nothing here depends on the real
        // cast, which design still owes.
        private static void WriteTestCharacter(SerializedProperty placements, string locationId)
        {
            placements.arraySize = 1;
            SerializedProperty entry = placements.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("characterId").stringValue = "arjun";
            entry.FindPropertyRelative("locationId").stringValue = locationId;
            entry.FindPropertyRelative("position").vector2Value = new Vector2(6f, 4f);
            entry.FindPropertyRelative("facing").enumValueIndex = (int)Facing.West;
            entry.FindPropertyRelative("conversationId").stringValue = "greybox_arjun_talk";
        }

        // Two stages, so the greybox exercises sequencing as well as a single objective: one keyed
        // on finishing a conversation, one on inspecting an object.
        private static QuestData BuildQuest()
        {
            var talk = new TalkCondition();
            talk.Configure(TalkCondition.With("greybox_arjun_talk", "arjun"));

            var look = new InspectCondition();
            look.Configure("greybox_tree");

            var quest = LoadOrCreate<QuestData>($"{DataFolder}/Quest_Greybox.asset");
            var serialized = new SerializedObject(quest);
            serialized.FindProperty("questId").stringValue = "greybox";
            serialized.FindProperty("displayName").stringValue = "Greybox";

            SerializedProperty objectives = serialized.FindProperty("objectives");
            objectives.arraySize = 2;
            objectives.GetArrayElementAtIndex(0).objectReferenceValue =
                BuildObjective("greybox_talk", "Talk to Arjun", talk);
            objectives.GetArrayElementAtIndex(1).objectReferenceValue =
                BuildObjective("greybox_look", "Look at the tree", look);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            return quest;
        }

        private static QuestObjective BuildObjective(string objectiveId, string text,
            ObjectiveCondition completion)
        {
            var objective = LoadOrCreate<QuestObjective>($"{DataFolder}/QuestObjective_{objectiveId}.asset");
            var serialized = new SerializedObject(objective);

            serialized.FindProperty("objectiveId").stringValue = objectiveId;
            serialized.FindProperty("displayText").stringValue = text;
            serialized.FindProperty("completion").managedReferenceValue = completion;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(objective);
            return objective;
        }

        // --- Placeholder conversations ---

        // --- Placeholder events ---

        // An opening beat that walks Arjun three tiles north before she gets control. Small, but it
        // exercises the whole chain: control is locked, a character walks a cardinal route through
        // the real collision map, and control comes back only once he has settled.
        private static void BuildEvents()
        {
            var opening = LoadOrCreate<HubEventData>($"{DataFolder}/Event_greybox_opening.asset");
            var serialized = new SerializedObject(opening);

            serialized.FindProperty("eventId").stringValue = "greybox_opening";
            serialized.FindProperty("trigger").enumValueIndex = (int)HubEventTrigger.VisitLoad;
            serialized.FindProperty("oneTime").boolValue = true;

            SerializedProperty actions = serialized.FindProperty("actions");
            actions.arraySize = 1;

            SerializedProperty walk = actions.GetArrayElementAtIndex(0);
            walk.FindPropertyRelative("kind").enumValueIndex = (int)HubEventActionKind.WalkCharacter;
            walk.FindPropertyRelative("targetId").stringValue = "arjun";
            walk.FindPropertyRelative("seconds").floatValue = 0f;

            SerializedProperty route = walk.FindPropertyRelative("route");
            route.arraySize = 1;
            route.GetArrayElementAtIndex(0).vector2Value = new Vector2(6f, 7f);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(opening);

            AppendUnique(LoadOrCreate<HubEventDatabase>($"{DataFolder}/HubEventDatabase.asset"), "events", opening);
        }

        // --- Plumbing ---

        private static void RegisterInCatalogs(HubVisitData visit, params HubLocationData[] locations)
        {
            var locationCatalog = LoadOrCreate<HubLocationDatabase>($"{DataFolder}/HubLocationDatabase.asset");
            foreach (HubLocationData location in locations)
                AppendUnique(locationCatalog, "locations", location);

            AppendUnique(LoadOrCreate<HubVisitDatabase>($"{DataFolder}/HubVisitDatabase.asset"), "visits", visit);
        }

        private static void AppendUnique(ScriptableObject catalog, string listName, Object entry)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty(listName);

            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == entry) return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Hub");
            EnsureFolder(Folder, "Greybox");
            EnsureFolder(Folder, "Data");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
