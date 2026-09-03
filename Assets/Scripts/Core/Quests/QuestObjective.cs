using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // One stage of a quest: the line she reads, what finishes it, and what it does.
    [CreateAssetMenu(fileName = "QuestObjective", menuName = "Project Astra/Quests/Objective")]
    public class QuestObjective : ScriptableObject
    {
        [SerializeField] private string objectiveId;

        [Tooltip("Short and action-oriented, e.g. \"Talk to the other students\".")]
        [SerializeField] private string displayText;

        [Tooltip("Show progress as 0/5 beside the text.")]
        [SerializeField] private bool showCounter;

        [SerializeReference] private ObjectiveCondition completion = new TalkCondition();

        [Tooltip("Run the moment this stage becomes active.")]
        [SerializeReference] private QuestEvent[] onStart = Array.Empty<QuestEvent>();

        [Tooltip("Run once this stage completes, before the next one is announced.")]
        [SerializeReference] private QuestEvent[] onComplete = Array.Empty<QuestEvent>();

        [Tooltip("Overrides where markers go. Leave empty to point at whatever the condition is still waiting on.")]
        [SerializeField] private string[] markerTargetIds = Array.Empty<string>();

        public string ObjectiveId => objectiveId;
        public string DisplayText => displayText;
        public bool ShowCounter => showCounter;
        public ObjectiveCondition Completion => completion;
        public QuestEvent[] OnStart => onStart;
        public QuestEvent[] OnComplete => onComplete;
        public string[] MarkerTargetIds => markerTargetIds;

        // Built without an asset file so the runner's tests need no fixture folder.
        public static QuestObjective Create(string objectiveId, ObjectiveCondition completion,
            string displayText = "Do the thing", bool showCounter = false,
            QuestEvent[] onStart = null, QuestEvent[] onComplete = null)
        {
            var objective = CreateInstance<QuestObjective>();
            objective.objectiveId = objectiveId;
            objective.displayText = displayText;
            objective.showCounter = showCounter;
            objective.completion = completion;
            objective.onStart = onStart ?? Array.Empty<QuestEvent>();
            objective.onComplete = onComplete ?? Array.Empty<QuestEvent>();
            return objective;
        }
    }
}
