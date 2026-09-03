using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // Lookup from a quest id to its asset, so a visit can name a quest without holding a reference.
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Project Astra/Quests/Quest Catalog")]
    public class QuestCatalog : ScriptableObject
    {
        [SerializeField] private QuestData[] quests = Array.Empty<QuestData>();

        public QuestData[] Quests => quests;

        public QuestData Get(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;

            foreach (QuestData quest in quests)
                if (quest != null && quest.QuestId == questId) return quest;
            return null;
        }
    }
}
