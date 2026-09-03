using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // One quest: its stages in the order they are played, and what it does at either end.
    [CreateAssetMenu(fileName = "QuestData", menuName = "Project Astra/Quests/Quest")]
    public class QuestData : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string displayName;

        [Tooltip("Worked through in this order. The next one starts only once the current one finishes.")]
        [SerializeField] private QuestObjective[] objectives = Array.Empty<QuestObjective>();

        [SerializeReference] private QuestEvent[] onStart = Array.Empty<QuestEvent>();
        [SerializeReference] private QuestEvent[] onComplete = Array.Empty<QuestEvent>();

        public string QuestId => questId;
        public string DisplayName => displayName;
        public QuestObjective[] Objectives => objectives;
        public QuestEvent[] OnStart => onStart;
        public QuestEvent[] OnComplete => onComplete;

        // Built without an asset file so the runner's tests need no fixture folder.
        public static QuestData Create(string questId, QuestObjective[] objectives,
            QuestEvent[] onStart = null, QuestEvent[] onComplete = null)
        {
            var quest = CreateInstance<QuestData>();
            quest.questId = questId;
            quest.displayName = questId;
            quest.objectives = objectives ?? Array.Empty<QuestObjective>();
            quest.onStart = onStart ?? Array.Empty<QuestEvent>();
            quest.onComplete = onComplete ?? Array.Empty<QuestEvent>();
            return quest;
        }
    }
}
