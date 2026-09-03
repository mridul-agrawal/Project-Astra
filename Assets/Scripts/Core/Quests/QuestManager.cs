using UnityEngine;
using ProjectAstra.Core.Events;

namespace ProjectAstra.Core.Quests
{
    // The quest system's one scene-facing part: it owns the runner, and carries signals in and
    // announcements out through EventService so nothing else has to know the runner exists.
    [DefaultExecutionOrder(-90)]
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Every quest in the game. A visit names the one it runs.")]
        [SerializeField] private QuestCatalog catalog;

        [Tooltip("What a quest event is allowed to touch. Found on this GameObject when left empty.")]
        [SerializeField] private MonoBehaviour worldBehaviour;

        public QuestProgress Progress { get; private set; }
        public QuestRunner Runner { get; private set; }

        public QuestObjective ActiveObjective => Runner?.ActiveObjective;
        public bool IsQuestComplete => Runner != null && Runner.IsQuestComplete;

        private void Awake()
        {
            Instance = this;
            Progress = new QuestProgress();
            Runner = new QuestRunner(Progress, ResolveWorld());
            Forward();
        }

        private void OnEnable() => EventService.Instance?.SubscribeGameplaySignal(OnGameplaySignal);

        private void OnDisable() => EventService.Instance?.UnsubscribeGameplaySignal(OnGameplaySignal);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Called once the visit's world is up, so a stage that opens on a scripted event has
        // something to act on.
        public bool BeginQuest(string questId)
        {
            QuestData quest = catalog != null ? catalog.Get(questId) : null;
            if (quest == null)
            {
                if (!string.IsNullOrEmpty(questId))
                    Debug.LogError($"[QuestManager] No quest '{questId}' in the catalog.", this);
                return false;
            }

            Runner.Begin(quest);
            return true;
        }

        private void OnGameplaySignal(GameplaySignal signal) => Runner.Report(signal);

        // The runner speaks plain C#; everyone else hears it through the facade.
        private void Forward()
        {
            Runner.QuestStarted += quest => EventService.Instance?.RaiseQuestStarted(quest);
            Runner.ObjectiveActivated += o => EventService.Instance?.RaiseObjectiveActivated(o);
            Runner.ObjectiveProgressed += o => EventService.Instance?.RaiseObjectiveProgressed(o);
            Runner.ObjectiveCompleted += o => EventService.Instance?.RaiseObjectiveCompleted(o);
            Runner.QuestCompleted += quest => EventService.Instance?.RaiseQuestCompleted(quest);
        }

        private IQuestWorld ResolveWorld()
        {
            if (worldBehaviour is IQuestWorld wired) return wired;

            var found = GetComponent<IQuestWorld>();
            if (found == null) Debug.LogError("[QuestManager] No IQuestWorld, so quest events will do nothing.", this);
            return found;
        }
    }
}
