using System;

namespace ProjectAstra.Core.Quests
{
    // Something a quest or an objective does when it starts or finishes. One class per kind.
    [Serializable]
    public abstract class QuestEvent
    {
        public abstract void Run(IQuestWorld world);
    }
}
