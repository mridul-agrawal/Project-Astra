using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Editor
{
    // What the hub is doing right now, and the levers to put it somewhere else.
    //
    // Everything here is a question a designer asks out loud while testing — which visit is this,
    // what am I meant to be doing, why is nothing happening when I press the button.
    public class HubDebugWindow : EditorWindow
    {
        private HubVisitData launchVisit;
        private int launchStage;
        private string conversation = "";
        private string gate = "";
        private Vector2 scroll;

        [MenuItem("Project Astra/Hub Testing")]
        public static void Open()
        {
            var window = GetWindow<HubDebugWindow>("Hub Testing");
            window.minSize = new Vector2(340, 420);
        }

        private void OnEnable() => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawStartHere();
            EditorGUILayout.Space(8);

            if (Application.isPlaying) DrawWhatIsHappening();
            else EditorGUILayout.HelpBox("Start a visit above to see what it is doing.", MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // --- Starting somewhere ---

        private void DrawStartHere()
        {
            EditorGUILayout.LabelField("Start in the middle of a visit", EditorStyles.boldLabel);

            HubVisitData[] visits = HubVisitLens.All().ToArray();
            if (visits.Length == 0)
            {
                EditorGUILayout.HelpBox("There are no visits yet.", MessageType.Info);
                return;
            }

            launchVisit = Pick(visits);
            launchStage = DrawStagePicker(launchVisit);

            using (new EditorGUI.DisabledScope(!HubLaunch.CanLaunch || launchVisit == null))
                if (GUILayout.Button("Play from here"))
                    HubLaunch.PlayFrom(launchVisit, launchStage, null);

            if (launchStage > 0)
                EditorGUILayout.HelpBox(
                    "Starting part-way means the earlier stages have not run. Whatever they would " +
                    "have opened or moved is still as the visit was authored.", MessageType.Warning);
        }

        private HubVisitData Pick(HubVisitData[] visits)
        {
            int current = Mathf.Max(0, System.Array.IndexOf(visits, launchVisit));
            int picked = EditorGUILayout.Popup("Visit", current, visits.Select(v => v.VisitId).ToArray());

            return visits[Mathf.Clamp(picked, 0, visits.Length - 1)];
        }

        private int DrawStagePicker(HubVisitData visit)
        {
            QuestObjective[] stages = StagesOf(visit);
            if (stages.Length == 0) return 0;

            string[] names = new[] { "from the start" }
                .Concat(stages.Select((stage, i) => $"{i + 1}. {stage.DisplayText}")).ToArray();

            return EditorGUILayout.Popup("Stage", Mathf.Clamp(launchStage, 0, names.Length - 1), names);
        }

        private static QuestObjective[] StagesOf(HubVisitData visit)
        {
            if (visit == null) return System.Array.Empty<QuestObjective>();

            QuestData quest = AssetDatabase.FindAssets("t:QuestData")
                .Select(g => AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(q => q != null && q.QuestId == visit.QuestId);

            return quest != null ? quest.Objectives : System.Array.Empty<QuestObjective>();
        }

        // --- What is happening ---

        private void DrawWhatIsHappening()
        {
            DrawWhereSheIs();
            EditorGUILayout.Space(6);
            DrawWhatSheIsDoing();
            EditorGUILayout.Space(6);
            DrawWhatIsInReach();
            EditorGUILayout.Space(6);
            DrawLevers();
        }

        private static void DrawWhereSheIs()
        {
            EditorGUILayout.LabelField("Where she is", EditorStyles.boldLabel);

            HubVisitService service = HubVisitService.Instance;
            if (service == null) { EditorGUILayout.LabelField("No visit is loaded."); return; }

            EditorGUILayout.LabelField("Visit", service.Visit != null ? service.Visit.VisitId : "none");
            EditorGUILayout.LabelField("Room", service.Location.CurrentLocationId ?? "none");

            HubPlayerController player = Object.FindFirstObjectByType<HubPlayerController>();
            EditorGUILayout.LabelField("Standing at",
                player != null ? ((Vector2)player.transform.position).ToString() : "not built yet");
        }

        private static void DrawWhatSheIsDoing()
        {
            EditorGUILayout.LabelField("What she is meant to be doing", EditorStyles.boldLabel);

            QuestManager quests = QuestManager.Instance;
            if (quests == null || quests.Runner == null) { EditorGUILayout.LabelField("No quest."); return; }

            QuestObjective active = quests.ActiveObjective;
            if (active == null) { EditorGUILayout.LabelField("Every stage is done."); return; }

            EditorGUILayout.LabelField("Stage", active.DisplayText);
            EditorGUILayout.LabelField("Progress", $"{quests.Runner.CurrentProgress} of {quests.Runner.RequiredProgress}");

            foreach (string waiting in quests.Runner.OutstandingTargets())
                EditorGUILayout.LabelField("   still waiting on", waiting);
        }

        // The question this window exists for: she is standing right there and the button does
        // nothing, and nothing in the game says why.
        private static void DrawWhatIsInReach()
        {
            EditorGUILayout.LabelField("What she could act on", EditorStyles.boldLabel);

            HubPlayerController player = Object.FindFirstObjectByType<HubPlayerController>();
            var her = player != null ? player.GetComponent<HubActor>() : null;
            if (her == null) { EditorGUILayout.LabelField("She is not there yet."); return; }

            var pose = new InteractorPose(her.Position, her.Facing);
            IInteractable chosen = player.Interaction.ResolvePriority(pose);

            if (player.Interaction.TargetsInRange.Count == 0)
            {
                EditorGUILayout.LabelField("Nothing is near enough to be a candidate.");
                return;
            }

            foreach (IInteractable candidate in player.Interaction.TargetsInRange)
                EditorGUILayout.LabelField($"   {HubReachReport.Name(candidate)}",
                    HubReachReport.Why(candidate, pose, chosen));
        }

        // --- Levers ---

        private void DrawLevers()
        {
            EditorGUILayout.LabelField("Make something happen", EditorStyles.boldLabel);

            DrawPlayAConversation();
            DrawForceAGate();
        }

        private void DrawPlayAConversation()
        {
            conversation = PickOne("Conversation", conversation, HubIdKind.Conversation);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(conversation)))
                if (GUILayout.Button("Play it")) Play(conversation);
        }

        // The same names the rest of the tools offer, so testing never means remembering one.
        private static string PickOne(string label, string current, HubIdKind kind)
        {
            string[] known = HubIds.Of(kind);
            if (known.Length == 0)
            {
                EditorGUILayout.LabelField(label, $"nothing is named a {kind} yet");
                return current;
            }

            int at = Mathf.Max(0, System.Array.IndexOf(known, current));
            return known[EditorGUILayout.Popup(label, at, known)];
        }

        private static void Play(string conversationId)
        {
            DialogueScript script = HubInteractionCatalog.Scripts != null
                ? HubInteractionCatalog.Scripts.Get(conversationId)
                : null;

            if (script == null)
            {
                Debug.LogError($"[Hub Testing] No conversation '{conversationId}' in the catalog.");
                return;
            }

            DialogueService.Instance?.Play(script, DialogueTriggeringContext.Conversation, null,
                HubVisitService.Instance?.Dialogue, InteractionEvents.RaiseFlag);
        }

        private void DrawForceAGate()
        {
            gate = PickOne("Gate", gate, HubIdKind.Gate);

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(gate) || HubVisitService.Instance == null))
            {
                if (GUILayout.Button("Open it")) HubVisitService.Instance.Flags.SetGate(gate, true);
                if (GUILayout.Button("Shut it")) HubVisitService.Instance.Flags.SetGate(gate, false);
            }
        }
    }
}
