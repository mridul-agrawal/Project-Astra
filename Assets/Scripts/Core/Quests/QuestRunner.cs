using System;
using System.Collections.Generic;

namespace ProjectAstra.Core.Quests
{
    // Walks a quest's stages in order and owns the only path by which progress is credited.
    public sealed class QuestRunner
    {
        private readonly QuestProgress progress;
        private readonly IQuestWorld world;

        // Signals that arrived while an earlier one was still being dealt with. A stage's own
        // events can raise a flag, and that must land on the stage that is active afterwards.
        private readonly Queue<GameplaySignal> pending = new();
        private bool settling;

        public event Action<QuestData> QuestStarted;
        public event Action<QuestObjective> ObjectiveActivated;
        public event Action<QuestObjective> ObjectiveProgressed;
        public event Action<QuestObjective> ObjectiveCompleted;
        public event Action<QuestData> QuestCompleted;

        public QuestRunner(QuestProgress progress, IQuestWorld world)
        {
            this.progress = progress;
            this.world = world;
        }

        public QuestData Quest { get; private set; }

        public QuestObjective ActiveObjective =>
            Quest != null && progress.ObjectiveIndex >= 0 && progress.ObjectiveIndex < Quest.Objectives.Length
                ? Quest.Objectives[progress.ObjectiveIndex]
                : null;

        public bool IsQuestComplete => Quest != null && ActiveObjective == null;

        public int CurrentProgress => progress.Tracker?.Current ?? 0;
        public int RequiredProgress => progress.Tracker?.Required ?? 0;
        public bool ShowsCounter => ActiveObjective != null && ActiveObjective.ShowCounter;

        // Call once the world is ready. Announces the opening stage and settles any run of stages
        // that finish the instant they start.
        public void Begin(QuestData quest)
        {
            Quest = quest;
            if (quest == null) return;

            progress.BeginQuest(quest.QuestId);
            QuestStarted?.Invoke(quest);
            Run(quest.OnStart);
            Activate();
        }

        // Puts a part-finished quest back where it was. Nothing is replayed — a stage that already
        // ran its opening events must not run them again on a reload.
        public void Resume(QuestData quest, QuestProgressDto saved)
        {
            Quest = quest;
            if (quest == null || saved == null) return;

            progress.Restore(saved);
            QuestObjective active = ActiveObjective;
            if (active == null) return;

            progress.BeginObjective(active);
            progress.RestoreTracker(saved.creditedTargetIds);
            ObjectiveActivated?.Invoke(active);
        }

        // The single entry point for progress. True only when this signal was new, wanted, and for
        // the stage that is active right now.
        public bool Report(GameplaySignal signal)
        {
            pending.Enqueue(signal);
            if (settling) return false;

            bool credited = false;
            settling = true;
            while (pending.Count > 0) credited |= Credit(pending.Dequeue());
            settling = false;
            return credited;
        }

        // What still needs doing on the active stage, for the markers.
        public IEnumerable<string> OutstandingTargets() =>
            progress.Tracker != null ? progress.Tracker.Outstanding() : Array.Empty<string>();

        public string MarkerFor(string targetId) => progress.Tracker?.MarkerFor(targetId);

        public bool HasCredited(string targetId) => progress.Tracker?.HasCredited(targetId) ?? false;

        private bool Credit(GameplaySignal signal)
        {
            QuestObjective active = ActiveObjective;
            if (active == null || progress.Tracker == null) return false;
            if (!progress.Tracker.TryCredit(signal)) return false;

            ObjectiveProgressed?.Invoke(active);
            if (progress.Tracker.IsSatisfied) Complete(active);
            return true;
        }

        // The spec's order: mark it done, apply what it changes, say so, then start the next.
        private void Complete(QuestObjective objective)
        {
            progress.CompleteObjective(objective.ObjectiveId);
            Run(objective.OnComplete);
            ObjectiveCompleted?.Invoke(objective);
            Activate();
        }

        private void Activate()
        {
            QuestObjective next = ActiveObjective;
            if (next == null)
            {
                Finish();
                return;
            }

            progress.BeginObjective(next);
            Run(next.OnStart);
            ObjectiveActivated?.Invoke(next);

            // A stage with nothing to wait for is already done, and a run of them settles here.
            if (progress.Tracker != null && progress.Tracker.IsSatisfied) Complete(next);
        }

        private void Finish()
        {
            progress.CompleteQuest(Quest.QuestId);
            Run(Quest.OnComplete);
            QuestCompleted?.Invoke(Quest);
        }

        private void Run(QuestEvent[] events)
        {
            if (events == null || world == null) return;

            foreach (QuestEvent authored in events) authored?.Run(world);
        }
    }
}
