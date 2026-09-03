using System;
using System.Collections.Generic;
using ProjectAstra.Core.Progression;

namespace ProjectAstra.Core.Quests
{
    // How far through a quest she is. Quest state and nothing else.
    public sealed class QuestProgress : IPersistable<QuestProgressDto>
    {
        private readonly List<string> completedObjectiveIds = new();
        private readonly List<string> completedQuestIds = new();

        public string QuestId { get; private set; }
        public int ObjectiveIndex { get; private set; }

        // The active stage's tracker. Replaced on every advance, so credit banked for one stage
        // can never be spent on the next.
        public ObjectiveTracker Tracker { get; private set; }

        public IReadOnlyList<string> CompletedObjectiveIds => completedObjectiveIds;
        public IReadOnlyList<string> CompletedQuestIds => completedQuestIds;

        public bool IsObjectiveCompleted(string objectiveId) => completedObjectiveIds.Contains(objectiveId);
        public bool IsQuestCompleted(string questId) => completedQuestIds.Contains(questId);

        public void BeginQuest(string questId)
        {
            QuestId = questId;
            ObjectiveIndex = 0;
            Tracker = null;
        }

        public void BeginObjective(QuestObjective objective)
        {
            Tracker = objective.Completion?.CreateTracker();
        }

        public void CompleteObjective(string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId) && !completedObjectiveIds.Contains(objectiveId))
                completedObjectiveIds.Add(objectiveId);

            ObjectiveIndex++;
            Tracker = null;
        }

        public void CompleteQuest(string questId)
        {
            if (!string.IsNullOrEmpty(questId) && !completedQuestIds.Contains(questId))
                completedQuestIds.Add(questId);
        }

        public QuestProgressDto Serialize() => new()
        {
            questId = QuestId,
            objectiveIndex = ObjectiveIndex,
            creditedTargetIds = Tracker != null ? new List<string>(Tracker.Credited) : new List<string>(),
            completedObjectiveIds = new List<string>(completedObjectiveIds),
            completedQuestIds = new List<string>(completedQuestIds)
        };

        // The runner re-activates the stage afterwards, which is what rebuilds the tracker; this
        // only puts back what cannot be worked out from the quest asset.
        public void Restore(QuestProgressDto dto)
        {
            if (dto == null) return;

            QuestId = dto.questId;
            ObjectiveIndex = dto.objectiveIndex;
            Replace(completedObjectiveIds, dto.completedObjectiveIds);
            Replace(completedQuestIds, dto.completedQuestIds);
        }

        public void RestoreTracker(IEnumerable<string> creditedTargetIds) =>
            Tracker?.Restore(creditedTargetIds);

        private static void Replace(List<string> target, List<string> source)
        {
            target.Clear();
            if (source != null) target.AddRange(source);
        }
    }

    [Serializable]
    public class QuestProgressDto
    {
        public string questId;
        public int objectiveIndex;
        public List<string> creditedTargetIds = new();
        public List<string> completedObjectiveIds = new();
        public List<string> completedQuestIds = new();
    }
}
