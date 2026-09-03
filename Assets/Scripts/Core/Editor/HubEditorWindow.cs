using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Editor
{
    // One window for everything about the hub that is not a position: which places exist, what
    // happens on each visit, the stages of its quest, and what is currently wrong.
    //
    // Rail picks the kind, the middle lists them, the right edits the one selected. The scene and
    // this window follow each other, so picking a room here shows it there and back again.
    public class HubEditorWindow : EditorWindow
    {
        private enum Tab { Rooms, Visits, Quests, Events, Search, Problems }

        private const float RailWidth = 110f;
        private const float ListWidth = 230f;

        private Tab tab = Tab.Rooms;
        private UnityEngine.Object selected;
        private UnityEditor.Editor embedded;
        private string filter = "";
        private Vector2 listScroll, detailScroll;
        private List<HubProblem> problems;
        private UnityEngine.Object pendingSelect;
        private HubIdKind searchKind = HubIdKind.Conversation;
        private string searching, renameTo = "";

        [MenuItem("Project Astra/Hub Editor")]
        public static void Open()
        {
            var window = GetWindow<HubEditorWindow>("Hub Editor");
            window.minSize = new Vector2(860, 540);
        }

        public static void OpenOnProblems()
        {
            var window = GetWindow<HubEditorWindow>("Hub Editor");
            window.Show(Tab.Problems);
        }

        private void OnEnable() => Selection.selectionChanged += FollowTheScene;

        private void OnDisable()
        {
            Selection.selectionChanged -= FollowTheScene;
            Forget();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRail();
                if (tab == Tab.Problems) DrawProblems();
                else if (tab == Tab.Search) DrawSearch();
                else { DrawList(); DrawDetail(); }
            }

            // Deferred so the selection never changes part-way through a layout pass.
            if (pendingSelect == null) return;
            Select(pendingSelect);
            pendingSelect = null;
        }

        // --- Rail ---

        private void DrawRail()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(RailWidth)))
            {
                EditorGUILayout.LabelField("Hub", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (Tab candidate in Enum.GetValues(typeof(Tab)))
                    if (GUILayout.Toggle(tab == candidate, candidate.ToString(), "Button") && tab != candidate)
                        Show(candidate);
            }
        }

        private void Show(Tab wanted)
        {
            tab = wanted;
            problems = null;
            Select(null);
        }

        // --- List ---

        private void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
            {
                DrawListHeader();

                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (UnityEngine.Object asset in Everything().Where(Matches))
                    if (GUILayout.Toggle(selected == asset, Label(asset), "Button") && selected != asset)
                        pendingSelect = asset;
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawListHeader()
        {
            EditorGUILayout.LabelField(tab.ToString(), EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                filter = EditorGUILayout.TextField(filter);
                if (GUILayout.Button("+ New", GUILayout.Width(56))) MakeOne();
            }
            EditorGUILayout.Space(2);
        }

        private void MakeOne()
        {
            HubIdKind kind = KindOf(tab);
            string id = HubAuthoring.Unused($"new_{kind.ToString().ToLowerInvariant()}", HubIds.Of(kind));

            pendingSelect = HubAssets.Create(kind, id);
        }

        // --- Detail ---

        private void DrawDetail()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (selected == null)
                {
                    EditorGUILayout.HelpBox("Pick one on the left, or make one with + New.", MessageType.None);
                    return;
                }

                DrawDetailHeader();

                detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                DrawWhatThisKindNeeds();
                DrawTheRest();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawDetailHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Label(selected), EditorStyles.boldLabel);

                if (selected is HubLocationData place && GUILayout.Button("Show in scene", GUILayout.Width(110)))
                    ShowInScene(place);

                if (GUILayout.Button("Reveal", GUILayout.Width(64))) EditorGUIUtility.PingObject(selected);
            }
        }

        private void DrawWhatThisKindNeeds()
        {
            if (selected is QuestData quest) DrawStages(quest);
            if (selected is HubVisitData visit) DrawTheQuestItRuns(visit);
        }

        // A quest's stages are drawn above as the ordered list they are, so showing the raw array
        // underneath would be the same thing twice, and only one of them is any good.
        private void DrawTheRest()
        {
            if (selected is QuestData) { DrawFieldsExcept(new SerializedObject(selected), "objectives"); return; }

            UnityEditor.Editor inspector = Inspector();
            if (inspector != null) inspector.OnInspectorGUI();
        }

        private static void DrawFieldsExcept(SerializedObject owner, params string[] skipped)
        {
            owner.Update();

            SerializedProperty field = owner.GetIterator();
            for (bool enterChildren = true; field.NextVisible(enterChildren); enterChildren = false)
            {
                if (field.name == "m_Script" || skipped.Contains(field.name)) continue;
                EditorGUILayout.PropertyField(field, true);
            }

            owner.ApplyModifiedProperties();
        }

        // A visit names its quest by id, so the way through to it should be a button rather than a
        // search.
        private void DrawTheQuestItRuns(HubVisitData visit)
        {
            QuestData quest = Quests().FirstOrDefault(candidate => candidate.QuestId == visit.QuestId);
            if (quest == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Runs the quest '{quest.QuestId}'", EditorStyles.miniLabel);
                if (GUILayout.Button("Open it", GUILayout.Width(70)))
                {
                    tab = Tab.Quests;
                    pendingSelect = quest;
                }
            }
            EditorGUILayout.Space(4);
        }

        // --- The stages of a quest ---

        private void DrawStages(QuestData quest)
        {
            var editable = new SerializedObject(quest);
            SerializedProperty stages = editable.FindProperty("objectives");

            EditorGUILayout.LabelField("Stages, in the order she works through them", EditorStyles.boldLabel);

            for (int i = 0; i < stages.arraySize; i++) DrawStage(stages, i);

            DrawAddStage(stages);
            editable.ApplyModifiedProperties();
            EditorGUILayout.Space(6);
        }

        private void DrawStage(SerializedProperty stages, int index)
        {
            var stage = stages.GetArrayElementAtIndex(index).objectReferenceValue as QuestObjective;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{index + 1}.", GUILayout.Width(24));
                if (GUILayout.Button(StageLabel(stage), EditorStyles.miniButton)) pendingSelect = stage;

                using (new EditorGUI.DisabledScope(index == 0))
                    if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                        stages.MoveArrayElement(index, index - 1);

                using (new EditorGUI.DisabledScope(index == stages.arraySize - 1))
                    if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                        stages.MoveArrayElement(index, index + 1);

                if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                    stages.DeleteArrayElementAtIndex(index);
            }
        }

        private void DrawAddStage(SerializedProperty stages)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ New stage", EditorStyles.miniButton, GUILayout.Width(90)))
                    AddStage(stages, HubAssets.Create(HubIdKind.Objective,
                        HubAuthoring.Unused("new_stage", HubIds.Of(HubIdKind.Objective))));

                if (GUILayout.Button("+ Existing stage", EditorStyles.miniButton, GUILayout.Width(110)))
                    OfferExistingStages(stages);
            }
        }

        private void OfferExistingStages(SerializedProperty stages)
        {
            var menu = new GenericMenu();
            SerializedObject owner = stages.serializedObject;
            string path = stages.propertyPath;

            foreach (QuestObjective candidate in Assets<QuestObjective>())
            {
                QuestObjective captured = candidate;
                menu.AddItem(new GUIContent(StageLabel(candidate)), false,
                    () => AddStage(owner.FindProperty(path), captured));
            }
            menu.ShowAsContext();
        }

        private static void AddStage(SerializedProperty stages, UnityEngine.Object stage)
        {
            if (stage == null) return;

            stages.arraySize++;
            stages.GetArrayElementAtIndex(stages.arraySize - 1).objectReferenceValue = stage;
            stages.serializedObject.ApplyModifiedProperties();
        }

        private static string StageLabel(QuestObjective stage)
        {
            if (stage == null) return "(missing)";
            return string.IsNullOrEmpty(stage.DisplayText) ? stage.ObjectiveId : stage.DisplayText;
        }

        // --- Where is this used ---

        // Every name in the hub, and everywhere each one is used. Renaming one changes it in all
        // of them, because doing that by hand means missing one and never finding out.
        private void DrawSearch()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawSearchHeader();

                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (string name in Named().Take(200)) DrawName(name);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSearchHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Every name, and where it is used", EditorStyles.boldLabel);
                searchKind = (HubIdKind)EditorGUILayout.EnumPopup(searchKind, GUILayout.Width(110));
            }

            filter = EditorGUILayout.TextField("Find", filter);
            EditorGUILayout.Space(2);
        }

        private IEnumerable<string> Named() =>
            HubIds.Of(searchKind).Where(name =>
                string.IsNullOrEmpty(filter) ||
                name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

        private void DrawName(string name)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawNameHeader(name);
                if (searching != name) return;

                foreach (HubUsage usage in HubUsages.Of(name)) DrawUsage(usage);
                DrawRenameTo(name);
            }
        }

        private void DrawNameHeader(string name)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool open = searching == name;
                if (GUILayout.Toggle(open, name, EditorStyles.miniButton) != open)
                {
                    searching = open ? null : name;
                    renameTo = name;
                }

                EditorGUILayout.LabelField($"used {HubUsages.CountOf(name)}×",
                    EditorStyles.miniLabel, GUILayout.Width(70));
            }
        }

        private void DrawUsage(HubUsage usage)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                EditorGUILayout.LabelField(usage.Reads, EditorStyles.miniLabel);

                if (GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(50)))
                    GoTo(usage.In);
            }
        }

        private void DrawRenameTo(string name)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                renameTo = EditorGUILayout.TextField("Rename to", renameTo);

                using (new EditorGUI.DisabledScope(!HubRename.CanRename(name, renameTo)))
                    if (GUILayout.Button("Everywhere", GUILayout.Width(90)))
                        ShowNotification(new GUIContent(HubRename.Everywhere(name, renameTo).Reads));
            }
        }

        // --- Problems ---

        private void DrawProblems()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("What is wrong right now", EditorStyles.boldLabel);
                    if (GUILayout.Button("Look again", GUILayout.Width(90))) problems = null;
                }

                problems ??= HubProblems.Collect();
                listScroll = EditorGUILayout.BeginScrollView(listScroll);

                if (problems.Count == 0)
                    EditorGUILayout.HelpBox("Nothing. Every name resolves and every room is reachable.",
                        MessageType.Info);

                foreach (HubProblem problem in problems) DrawProblem(problem);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawProblem(HubProblem problem)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(problem.Message, MessageType.Warning);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(150)))
                {
                    using (new EditorGUI.DisabledScope(problem.Asset == null))
                        if (GUILayout.Button("Take me there")) GoTo(problem.Asset);

                    if (problem.Fix != null && GUILayout.Button(problem.Fix.Label)) Repair(problem);
                }
            }
        }

        private void Repair(HubProblem problem)
        {
            problem.Fix.Apply();
            problems = null;
            HubWatch.LookAgainSoon();
        }

        private void GoTo(UnityEngine.Object asset)
        {
            if (TabFor(asset, out Tab found)) tab = found;
            pendingSelect = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // --- Following the scene ---

        // Clicking anything in a room here shows that room's row, so the two views never disagree
        // about what is being worked on.
        private void FollowTheScene()
        {
            if (Selection.activeGameObject == null) return;

            var room = Selection.activeGameObject.GetComponentInParent<HubRoom>();
            if (room == null || room.Location == null || selected == room.Location) return;

            tab = Tab.Rooms;
            pendingSelect = room.Location;
            Repaint();
        }

        private static void ShowInScene(HubLocationData place)
        {
            HubRoom room = HubRooms.InLoadedScenes().FirstOrDefault(candidate => candidate.Location == place);
            if (room == null)
            {
                Debug.LogWarning($"[Hub Editor] No room in the open scene is '{place.LocationId}'.");
                return;
            }

            HubEditing.Isolate(room);
            Selection.activeGameObject = room.gameObject;
        }

        // --- Lookups ---

        private void Select(UnityEngine.Object asset)
        {
            selected = asset;
            Forget();
        }

        // Built when it is needed rather than when something is picked, because a recompile throws
        // the editor away while the asset it was showing survives.
        private UnityEditor.Editor Inspector()
        {
            if (selected == null) { Forget(); return null; }
            if (embedded != null && embedded.target == selected) return embedded;

            Forget();
            return embedded = UnityEditor.Editor.CreateEditor(selected);
        }

        private void Forget()
        {
            if (embedded == null) return;
            DestroyImmediate(embedded);
            embedded = null;
        }

        private bool Matches(UnityEngine.Object asset) =>
            string.IsNullOrEmpty(filter) ||
            Label(asset).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private IEnumerable<UnityEngine.Object> Everything() => tab switch
        {
            Tab.Rooms => Assets<HubLocationData>(),
            Tab.Visits => Assets<HubVisitData>(),
            Tab.Quests => Quests(),
            Tab.Events => Assets<HubEventData>(),
            _ => Enumerable.Empty<UnityEngine.Object>()
        };

        private static IEnumerable<QuestData> Quests() => Assets<QuestData>();

        private static IEnumerable<T> Assets<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase);

        // The name the thing calls itself, which is what everything else refers to it by.
        private static string Label(UnityEngine.Object asset) => asset switch
        {
            HubLocationData place => Named(place.DisplayName, place.LocationId, place.name),
            HubVisitData visit => Named(visit.DisplayName, visit.VisitId, visit.name),
            QuestData quest => Named(quest.DisplayName, quest.QuestId, quest.name),
            QuestObjective stage => Named(stage.DisplayText, stage.ObjectiveId, stage.name),
            HubEventData authored => Named(null, authored.EventId, authored.name),
            _ => asset != null ? asset.name : "(none)"
        };

        private static string Named(string shown, string id, string file) =>
            !string.IsNullOrWhiteSpace(shown) ? shown
            : !string.IsNullOrWhiteSpace(id) ? id
            : file;

        private static HubIdKind KindOf(Tab tab) => tab switch
        {
            Tab.Rooms => HubIdKind.Location,
            Tab.Visits => HubIdKind.Visit,
            Tab.Quests => HubIdKind.Quest,
            Tab.Events => HubIdKind.Event,
            _ => HubIdKind.Location
        };

        private static bool TabFor(UnityEngine.Object asset, out Tab tab)
        {
            switch (asset)
            {
                case HubLocationData: tab = Tab.Rooms; return true;
                case HubVisitData: tab = Tab.Visits; return true;
                case QuestData: case QuestObjective: tab = Tab.Quests; return true;
                case HubEventData: tab = Tab.Events; return true;
                default: tab = Tab.Rooms; return false;
            }
        }
    }
}
