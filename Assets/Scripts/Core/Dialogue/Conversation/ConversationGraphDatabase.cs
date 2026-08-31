using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue.Conversation
{
    // Lookup from a conversation id to its graph, so a character placement or an interactable can
    // name a conversation without holding a direct reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "ConversationGraphDatabase", menuName = "Project Astra/Dialogue/Conversation Database")]
    public class ConversationGraphDatabase : ScriptableObject
    {
        [SerializeField] private List<ConversationGraphData> conversations = new();

        private Dictionary<string, ConversationGraphData> byId;

        public ConversationGraphData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out ConversationGraphData graph) ? graph : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, ConversationGraphData>();
            foreach (ConversationGraphData graph in conversations)
                if (graph != null && !string.IsNullOrEmpty(graph.ConversationId))
                    byId[graph.ConversationId] = graph;
        }
    }
}
