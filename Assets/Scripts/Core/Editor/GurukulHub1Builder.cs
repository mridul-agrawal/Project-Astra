using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Gurukul.Conversation;
using ProjectAstra.Core.Gurukul.Events;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Builds Hub Interaction 1 as the spec's sequence: speak to the other five students, collect the
    // report card from the Guru, then the training-ground beat that leads into Map 1.
    //
    // The SHAPE here is real — the objectives, the counter, the branching, the departure checks are
    // all the shipping systems. The CONTENT is not. Every line reads PLACEHOLDER, and the cast all
    // stand in the greybox courtyard rather than the authored Gurukul, because neither the map nor
    // the script exists yet. docs/hub/hub1-content-owed.md lists exactly what design still owes;
    // the spec is explicit that programmers don't invent any of it.
    public static class GurukulHub1Builder
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

        [MenuItem("Project Astra/Gurukul/Build Hub Interaction 1 (Placeholder)")]
        public static void Build()
        {
            EnsureFolder("Assets/Gurukul", "Hub1");

            BuildCast();
            List<ConversationGraphData> conversations = BuildConversations();
            GurukulEventData trainingGround = BuildTrainingGroundEvent();
            GurukulVisitData visit = BuildVisit(trainingGround);

            RegisterConversations(conversations);
            RegisterVisit(visit);
            RegisterEvent(trainingGround);
            InsertCampaignStep(visit);

            AssetDatabase.SaveAssets();
            Selection.activeObject = visit;
            Debug.Log("[GurukulHub1] Built Hub Interaction 1 with placeholder content. " +
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

        private static List<ConversationGraphData> BuildConversations()
        {
            var built = new List<ConversationGraphData>();

            foreach ((string id, string name, _, _) in Students)
                built.Add(StudentConversation(id, name));

            built.Add(ReportCardConversation());
            return built;
        }

        // First time counts toward the counter; afterwards they have a shorter line, which is the
        // spec's first-time-versus-repeat rule.
        private static ConversationGraphData StudentConversation(string unitId, string name)
        {
            DialogueScript first = GurukulPlaceholderDialogue.Script(DataFolder, $"hub1_{unitId}_first",
                $"PLACEHOLDER: {name} says something about the report cards.");
            DialogueScript again = GurukulPlaceholderDialogue.Script(DataFolder, $"hub1_{unitId}_again",
                $"PLACEHOLDER: {name} has already said their piece.");

            var nodes = new[]
            {
                ScriptNode("first", first, null),
                ScriptNode("again", again, null)
            };
            return GurukulPlaceholderDialogue.Graph(DataFolder, $"hub1_talk_{unitId}", "first", nodes,
                repeatEntryNodeId: "again");
        }

        private static ConversationGraphData ReportCardConversation()
        {
            DialogueScript handover = GurukulPlaceholderDialogue.Script(DataFolder, "hub1_report_card",
                "PLACEHOLDER: the Guru hands over the report card.",
                "PLACEHOLDER: he says she cannot join external missions until she relearns the basics.",
                "PLACEHOLDER: he agrees to the practical retest.");

            var nodes = new[] { ScriptNode("handover", handover, null) };
            return GurukulPlaceholderDialogue.Graph(DataFolder, "hub1_report_card", "handover", nodes);
        }

        // --- Event and objectives ---

        // The spec's automatic departure: the retest is agreed, the scene moves to the training
        // ground, and Map 1 begins without control ever coming back.
        private static GurukulEventData BuildTrainingGroundEvent()
        {
            DialogueScript scene = GurukulPlaceholderDialogue.Script(DataFolder, "hub1_training_ground",
                "PLACEHOLDER: the scene moves to the Training Grounds.",
                "PLACEHOLDER: the Guru conjures two shadow puppets; the others gather to watch.");

            var authored = LoadOrCreate<GurukulEventData>($"{DataFolder}/Event_hub1_training_ground.asset");
            var serialized = new SerializedObject(authored);

            serialized.FindProperty("eventId").stringValue = "hub1_training_ground";
            serialized.FindProperty("trigger").enumValueIndex = (int)GurukulEventTrigger.Called;
            serialized.FindProperty("oneTime").boolValue = true;

            SerializedProperty actions = serialized.FindProperty("actions");
            actions.arraySize = 2;
            WriteAction(actions.GetArrayElementAtIndex(0), GurukulEventActionKind.PlayConversation,
                valueId: "hub1_training_scene");
            WriteAction(actions.GetArrayElementAtIndex(1), GurukulEventActionKind.Depart,
                valueId: DestinationMap);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(authored);

            GurukulPlaceholderDialogue.Graph(DataFolder, "hub1_training_scene", "scene",
                new[] { ScriptNode("scene", scene, null) });
            return authored;
        }

        private static void WriteAction(SerializedProperty action, GurukulEventActionKind kind,
            string targetId = "", string valueId = "")
        {
            action.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            action.FindPropertyRelative("targetId").stringValue = targetId;
            action.FindPropertyRelative("valueId").stringValue = valueId;
            action.FindPropertyRelative("route").arraySize = 0;
            action.FindPropertyRelative("seconds").floatValue = 0f;
        }

        private static GurukulObjectiveData BuildTalkObjective()
        {
            var objective = LoadOrCreate<GurukulObjectiveData>($"{DataFolder}/Objective_hub1_students.asset");
            var serialized = new SerializedObject(objective);

            serialized.FindProperty("objectiveId").stringValue = "hub1_students";
            serialized.FindProperty("displayText").stringValue = "Talk to the other students";

            SerializedProperty completion = serialized.FindProperty("completion");
            completion.FindPropertyRelative("kind").enumValueIndex = (int)GurukulConditionKind.ConversationCompleted;
            completion.FindPropertyRelative("showCounter").boolValue = true;

            // Five equally valid targets, clearable in any order — the spec's 0/5 counter.
            SerializedProperty targets = completion.FindPropertyRelative("targetIds");
            SerializedProperty markers = serialized.FindProperty("markerTargetIds");
            targets.arraySize = Students.Length;
            markers.arraySize = Students.Length;

            for (int i = 0; i < Students.Length; i++)
            {
                targets.GetArrayElementAtIndex(i).stringValue = $"hub1_talk_{Students[i].id}";
                markers.GetArrayElementAtIndex(i).stringValue = Students[i].id;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(objective);
            return objective;
        }

        private static GurukulObjectiveData BuildReportCardObjective(GurukulEventData trainingGround)
        {
            var objective = LoadOrCreate<GurukulObjectiveData>($"{DataFolder}/Objective_hub1_report_card.asset");
            var serialized = new SerializedObject(objective);

            serialized.FindProperty("objectiveId").stringValue = "hub1_report_card";
            serialized.FindProperty("displayText").stringValue = "Collect your report card";

            SerializedProperty completion = serialized.FindProperty("completion");
            completion.FindPropertyRelative("kind").enumValueIndex = (int)GurukulConditionKind.ConversationCompleted;
            completion.FindPropertyRelative("showCounter").boolValue = false;

            SerializedProperty targets = completion.FindPropertyRelative("targetIds");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).stringValue = "hub1_report_card";

            SerializedProperty markers = serialized.FindProperty("markerTargetIds");
            markers.arraySize = 1;
            markers.GetArrayElementAtIndex(0).stringValue = "guru";

            // Finishing the report card runs straight into the training ground, so control never
            // returns to a hub with nothing left to do in it.
            SerializedProperty effects = serialized.FindProperty("onComplete");
            effects.arraySize = 1;
            SerializedProperty fire = effects.GetArrayElementAtIndex(0);
            fire.FindPropertyRelative("kind").enumValueIndex = (int)GurukulEffectKind.FireEvent;
            fire.FindPropertyRelative("valueId").stringValue = trainingGround.EventId;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(objective);
            return objective;
        }

        // --- Visit ---

        private static GurukulVisitData BuildVisit(GurukulEventData trainingGround)
        {
            var visit = LoadOrCreate<GurukulVisitData>($"{DataFolder}/Visit_Hub1.asset");
            var serialized = new SerializedObject(visit);

            serialized.FindProperty("visitId").stringValue = "hub1";
            serialized.FindProperty("displayName").stringValue = "Hub Interaction 1";
            serialized.FindProperty("startLocationId").stringValue = Courtyard;
            serialized.FindProperty("playerSpawn").vector2Value = new Vector2(4f, 4f);
            serialized.FindProperty("playerFacing").enumValueIndex = (int)Facing.South;

            WritePlacements(serialized.FindProperty("characterPlacements"));
            WriteObjectives(serialized.FindProperty("objectives"), trainingGround);
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

        private static void WriteObjectives(SerializedProperty list, GurukulEventData trainingGround)
        {
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = BuildTalkObjective();
            list.GetArrayElementAtIndex(1).objectReferenceValue = BuildReportCardObjective(trainingGround);
        }

        private static void WriteDeparture(SerializedProperty departure)
        {
            departure.FindPropertyRelative("destinationMapId").stringValue = DestinationMap;
            departure.FindPropertyRelative("mode").enumValueIndex = (int)GurukulDepartureMode.Automatic;
            departure.FindPropertyRelative("departureTargetId").stringValue = "";
        }

        // --- Registration ---

        private static void RegisterConversations(List<ConversationGraphData> conversations)
        {
            var catalog = FindAsset<ConversationGraphDatabase>();
            if (catalog == null) return;

            foreach (ConversationGraphData graph in conversations) AppendUnique(catalog, "conversations", graph);
            AppendUnique(catalog, "conversations",
                AssetDatabase.LoadAssetAtPath<ConversationGraphData>($"{DataFolder}/Conversation_hub1_training_scene.asset"));
        }

        private static void RegisterVisit(GurukulVisitData visit) =>
            AppendUnique(FindAsset<GurukulVisitDatabase>(), "visits", visit);

        private static void RegisterEvent(GurukulEventData authored) =>
            AppendUnique(FindAsset<GurukulEventDatabase>(), "events", authored);

        // Slotted in immediately before the battle it departs to, so the opening cutscene still
        // plays first and the campaign reads intro, hub, battle.
        private static void InsertCampaignStep(GurukulVisitData visit)
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
                Debug.LogWarning($"[GurukulHub1] The campaign has no '{DestinationMap}' battle to depart to — " +
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

        private static ConversationNode ScriptNode(string id, DialogueScript script, string next) => new()
        {
            nodeId = id, kind = ConversationNodeKind.Script, script = script, nextNodeId = next
        };

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
