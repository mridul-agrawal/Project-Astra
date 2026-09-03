using System;
using UnityEngine;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Events
{
    // ScriptableObject event bus for what the quest system announces. Reached only through
    // EventService; listeners are the objective HUD, the markers, and anything the world gates on.
    [CreateAssetMenu(fileName = "QuestEventChannel",
        menuName = "Project Astra/Core/Quest Event Channel")]
    public class QuestEventChannel : ScriptableObject
    {
        private Action<QuestData> onQuestStarted;
        private Action<ObjectiveStatus> onObjectiveActivated;
        private Action<ObjectiveStatus> onObjectiveProgressed;
        private Action<QuestObjective> onObjectiveCompleted;
        private Action<QuestData> onQuestCompleted;

        public void RegisterQuestStarted(Action<QuestData> listener) => onQuestStarted += listener;
        public void UnregisterQuestStarted(Action<QuestData> listener) => onQuestStarted -= listener;
        public void RaiseQuestStarted(QuestData quest) => onQuestStarted?.Invoke(quest);

        public void RegisterObjectiveActivated(Action<ObjectiveStatus> listener) => onObjectiveActivated += listener;
        public void UnregisterObjectiveActivated(Action<ObjectiveStatus> listener) => onObjectiveActivated -= listener;
        public void RaiseObjectiveActivated(ObjectiveStatus status) => onObjectiveActivated?.Invoke(status);

        public void RegisterObjectiveProgressed(Action<ObjectiveStatus> listener) => onObjectiveProgressed += listener;
        public void UnregisterObjectiveProgressed(Action<ObjectiveStatus> listener) => onObjectiveProgressed -= listener;
        public void RaiseObjectiveProgressed(ObjectiveStatus status) => onObjectiveProgressed?.Invoke(status);

        public void RegisterObjectiveCompleted(Action<QuestObjective> listener) => onObjectiveCompleted += listener;
        public void UnregisterObjectiveCompleted(Action<QuestObjective> listener) => onObjectiveCompleted -= listener;
        public void RaiseObjectiveCompleted(QuestObjective objective) => onObjectiveCompleted?.Invoke(objective);

        public void RegisterQuestCompleted(Action<QuestData> listener) => onQuestCompleted += listener;
        public void UnregisterQuestCompleted(Action<QuestData> listener) => onQuestCompleted -= listener;
        public void RaiseQuestCompleted(QuestData quest) => onQuestCompleted?.Invoke(quest);
    }
}
