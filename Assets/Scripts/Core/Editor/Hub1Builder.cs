using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Builds Hub Interaction 1's placeholder content: five students, the report card, the departure.
    // Every line reads PLACEHOLDER; docs/hub/hub1-content-owed.md lists what design still owes.
    public static class Hub1Builder
    {
        private const string DataFolder = "Assets/Gurukul/Hub1";
        private const string UnitFolder = "Assets/ScriptableObjects/Units/Characters";

        private const string Courtyard = "greybox_courtyard";
        private const string House = "greybox_house";
        private const string DestinationMap = "tooltesting map";

        // The spec's locked Hub 1 cast. Positions are greybox stand-ins for the authored placements
        // (Kaal in the Library, Aryaman at the Training Grounds, and so on).
        private static readonly (string id, string name, Vector2 at, Facing facing)[] Students =
        {
            ("kaal", "Kaal", new Vector2(6f, 8f), Facing.South),
            ("aryaman", "Aryaman", new Vector2(10f, 4f), Facing.West),
            ("madhavi", "Madhavi", new Vector2(6f, 14f), Facing.South),
            ("rudrani", "Rudrani", new Vector2(11f, 10f), Facing.West),
            ("gajendra", "Gajendra", new Vector2(18f, 8f), Facing.West),
        };

        [MenuItem("Project Astra/Hub/Build Hub Interaction 1 (Placeholder)")]
        public static void Build()
        {
            EnsureFolder("Assets/Gurukul", "Hub1");

            BuildCast();
            HubEventData trainingGround = BuildTrainingGroundEvent();
            HubVisitData visit = BuildVisit(trainingGround);

            RegisterVisit(visit);
            RegisterEvent(trainingGround);
            InsertCampaignStep(visit);

            AssetDatabase.SaveAssets();
            Selection.activeObject = visit;
            Debug.Log("[Hub1] Built Hub Interaction 1 with placeholder content. " +
                      "See docs/hub/hub1-content-owed.md for what design still owes.");
        }

        // --- Cast ---

        // Seven new definitions on the placeholder animation set. The spec's names win over the
        // existing units, which are a different cast for a different draft of the story.
        private static void BuildCast()
        {
            UnitDefinition template = Load<UnitDefinition>($"{UnitFolder}/Aranya.asset");
            var database = FindAsset<UnitDatabase>();

            foreach ((string id, string name, _, _) in Students)
                Register(database, BuildUnit(id, name, template));

            Register(database, BuildUnit("guru", "Guru", template));
            Register(database, BuildUnit("merchant", "Merchant", template));
        }

        private static UnitDefinition BuildUnit(string unitId, string unitName, UnitDefinition template)
        {
            string path = $"{UnitFolder}/{unitName}.asset";
            var unit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
            if (unit == null)
            {
                unit = ScriptableObject.CreateInstance<UnitDefinition>();
                AssetDatabase.CreateAsset(unit, path);
            }

            var serialized = new SerializedObject(unit);
            serialized.FindProperty("unitName").stringValue = unitName;
            serialized.FindProperty("unitId").stringValue = unitId;
            serialized.FindProperty("oneLineIdentity").stringValue = "PLACEHOLDER — design owns this character.";

            // Borrowed from the protagonist so they are visible and animate; both are placeholders
            // and the Data Hub's problem list will keep flagging the missing portrait.
            CopyReference(serialized, template, "mapSprite");
            CopyReference(serialized, template, "mapAnimator");
            CopyReference(serialized, template, "defaultClass");

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(unit);
            return unit;
        }

        private static void CopyReference(SerializedObject target, UnitDefinition template, string field)
        {
            var from = new SerializedObject(template);
            target.FindProperty(field).objectReferenceValue = from.FindProperty(field).objectReferenceValue;
        }

        private static void Register(UnitDatabase database, UnitDefinition unit)
        {
            if (database == null) return;
            AppendUnique(database, "units", unit);
        }

        // --- Conversations ---

        // --- Event and objectives ---

        // The spec's automatic departure: the retest is agreed, the scene moves to the training
        // ground, and Map 1 begins without control ever coming back.
        private static HubEventData BuildTrainingGroundEvent()
        {
            var authored = LoadOrCreate<HubEventData>($"{DataFolder}/Event_hub1_training_ground.asset");
            var serialized = new SerializedObject(authored);

            serialized.FindProperty("eventId").stringValue = "hub1_training_ground";
            serialized.FindProperty("trigger").enumValueIndex = (int)HubEventTrigger.Called;
            serialized.FindProperty("oneTime").boolValue = true;

            SerializedProperty actions = serialized.FindProperty("actions");
            actions.arraySize = 2;
            WriteAction(actions.GetArrayElementAtIndex(0), HubEventActionKind.PlayConversation,
                valueId: "hub1_training_scene");
            WriteAction(actions.GetArrayElementAtIndex(1), HubEventActionKind.Depart,
                valueId: DestinationMap);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(authored);

            return authored;
        }

        private static void WriteAction(SerializedProperty action, HubEventActionKind kind,
            string targetId = "", string valueId = "")
        {
            action.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            action.FindPropertyRelative("targetId").stringValue = targetId;
            action.FindPropertyRelative("valueId").stringValue = valueId;
            action.FindPropertyRelative("route").arraySize = 0;
            action.FindPropertyRelative("seconds").floatValue = 0f;
        }

        // Five equally valid targets, clearable in any order — the spec's 0/5 counter. The
        // character on each target is only so a marker has someone to stand over.
        private static QuestObjective BuildTalkObjective()
        {
            var students = new TalkCondition.Target[Students.Length];
            for (int i = 0; i < Students.Length; i++)
                students[i] = TalkCondition.With($"hub1_talk_{Students[i].id}", Students[i].id);

            var condition = new TalkCondition();
            condition.Configure(students);

            return WriteObjective("Objective_hub1_students", "hub1_students",
                "Talk to the other students", condition, showCounter: true);
        }

        // Finishing the report card runs straight into the training ground, so control never
        // returns to a hub with nothing left to do in it.
        private static QuestObjective BuildReportCardObjective(HubEventData trainingGround)
        {
            var condition = new TalkCondition();
            condition.Configure(TalkCondition.With("hub1_report_card", "guru"));

            var training = new PlayAuthoredEventEvent();
            training.Configure(trainingGround.EventId);

            return WriteObjective("Objective_hub1_report_card", "hub1_report_card",
                "Collect your report card", condition, showCounter: false,
                onComplete: new QuestEvent[] { training });
        }

        private static QuestObjective WriteObjective(string assetName, string objectiveId, string text,
            ObjectiveCondition completion, bool showCounter, QuestEvent[] onComplete = null)
        {
            var objective = LoadOrCreate<QuestObjective>($"{DataFolder}/{assetName}.asset");
            var serialized = new SerializedObject(objective);

            serialized.FindProperty("objectiveId").stringValue = objectiveId;
            serialized.FindProperty("displayText").stringValue = text;
            serialized.FindProperty("showCounter").boolValue = showCounter;
            serialized.FindProperty("completion").managedReferenceValue = completion;

            SerializedProperty effects = serialized.FindProperty("onComplete");
            effects.arraySize = onComplete?.Length ?? 0;
            for (int i = 0; i < effects.arraySize; i++)
                effects.GetArrayElementAtIndex(i).managedReferenceValue = onComplete[i];

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(objective);
            return objective;
        }

        private static QuestData BuildQuest(HubEventData trainingGround)
        {
            var quest = LoadOrCreate<QuestData>($"{DataFolder}/Quest_Hub1.asset");
            var serialized = new SerializedObject(quest);

            serialized.FindProperty("questId").stringValue = "hub1";
            serialized.FindProperty("displayName").stringValue = "Hub Interaction 1";

            SerializedProperty objectives = serialized.FindProperty("objectives");
            objectives.arraySize = 2;
            objectives.GetArrayElementAtIndex(0).objectReferenceValue = BuildTalkObjective();
            objectives.GetArrayElementAtIndex(1).objectReferenceValue = BuildReportCardObjective(trainingGround);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);
            return quest;
        }

        // --- Visit ---

        private static HubVisitData BuildVisit(HubEventData trainingGround)
        {
            var visit = LoadOrCreate<HubVisitData>($"{DataFolder}/Visit_Hub1.asset");
            var serialized = new SerializedObject(visit);

            serialized.FindProperty("visitId").stringValue = "hub1";
            serialized.FindProperty("displayName").stringValue = "Hub Interaction 1";
            serialized.FindProperty("startLocationId").stringValue = Courtyard;
            serialized.FindProperty("playerSpawn").vector2Value = new Vector2(4f, 4f);
            serialized.FindProperty("playerFacing").enumValueIndex = (int)Facing.South;

            WritePlacements(serialized.FindProperty("characterPlacements"));
            serialized.FindProperty("questId").stringValue = BuildQuest(trainingGround).QuestId;
            WriteDeparture(serialized.FindProperty("departure"));

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(visit);
            return visit;
        }

        private static void WritePlacements(SerializedProperty placements)
        {
            placements.arraySize = Students.Length + 1;

            for (int i = 0; i < Students.Length; i++)
            {
                (string id, _, Vector2 at, Facing facing) = Students[i];
                WritePlacement(placements.GetArrayElementAtIndex(i), id, Courtyard, at, facing, $"hub1_talk_{id}");
            }

            // Standing in for the Guru's Quarters until the real interiors exist.
            WritePlacement(placements.GetArrayElementAtIndex(Students.Length), "guru", House,
                new Vector2(8f, 5f), Facing.South, "hub1_report_card");
        }

        private static void WritePlacement(SerializedProperty entry, string characterId, string locationId,
            Vector2 position, Facing facing, string conversationId)
        {
            entry.FindPropertyRelative("characterId").stringValue = characterId;
            entry.FindPropertyRelative("locationId").stringValue = locationId;
            entry.FindPropertyRelative("position").vector2Value = position;
            entry.FindPropertyRelative("facing").enumValueIndex = (int)facing;
            entry.FindPropertyRelative("conversationId").stringValue = conversationId;
        }

        private static void WriteDeparture(SerializedProperty departure)
        {
            departure.FindPropertyRelative("destinationMapId").stringValue = DestinationMap;
            departure.FindPropertyRelative("mode").enumValueIndex = (int)HubDepartureMode.Automatic;
            departure.FindPropertyRelative("departureTargetId").stringValue = "";
        }

        // --- Registration ---

        private static void RegisterVisit(HubVisitData visit) =>
            AppendUnique(FindAsset<HubVisitDatabase>(), "visits", visit);

        private static void RegisterEvent(HubEventData authored) =>
            AppendUnique(FindAsset<HubEventDatabase>(), "events", authored);

        // Slotted in immediately before the battle it departs to, so the opening cutscene still
        // plays first and the campaign reads intro, hub, battle.
        private static void InsertCampaignStep(HubVisitData visit)
        {
            var campaign = FindAsset<Campaign>();
            if (campaign == null) return;

            var serialized = new SerializedObject(campaign);
            SerializedProperty steps = serialized.FindProperty("steps");

            for (int i = 0; i < steps.arraySize; i++)
                if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("visitId").stringValue == visit.VisitId) return;

            int battleIndex = IndexOfBattle(steps, DestinationMap);
            if (battleIndex < 0)
            {
                Debug.LogWarning($"[Hub1] The campaign has no '{DestinationMap}' battle to depart to — " +
                                 "add the hub step by hand once it does.");
                return;
            }

            steps.InsertArrayElementAtIndex(battleIndex);
            SerializedProperty step = steps.GetArrayElementAtIndex(battleIndex);
            step.FindPropertyRelative("kind").enumValueIndex = (int)CampaignStepKind.HubVisit;
            step.FindPropertyRelative("visitId").stringValue = visit.VisitId;
            step.FindPropertyRelative("mapId").stringValue = "";

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(campaign);
        }

        private static int IndexOfBattle(SerializedProperty steps, string mapId)
        {
            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(i);
                if ((CampaignStepKind)step.FindPropertyRelative("kind").enumValueIndex != CampaignStepKind.Battle) continue;
                if (step.FindPropertyRelative("mapId").stringValue == mapId) return i;
            }
            return -1;
        }

        // --- Plumbing ---

        private static void AppendUnique(Object catalog, string listName, Object entry)
        {
            if (catalog == null || entry == null) return;

            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty(listName);

            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == entry) return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static T FindAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
