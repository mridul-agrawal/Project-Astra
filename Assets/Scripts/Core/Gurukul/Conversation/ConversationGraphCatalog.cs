using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul.Conversation
{
    // Lookup from a conversation id to its graph, so a character placement or an interactable can
    // name a conversation without holding a direct reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "ConversationGraphCatalog", menuName = "Project Astra/Gurukul/Conversation Catalog")]
    public class ConversationGraphCatalog : ScriptableObject
    {
        [SerializeField] private List<ConversationGraph> conversations = new();

        private Dictionary<string, ConversationGraph> byId;

        public ConversationGraph Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out ConversationGraph graph) ? graph : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, ConversationGraph>();
            foreach (ConversationGraph graph in conversations)
                if (graph != null && !string.IsNullOrEmpty(graph.ConversationId))
                    byId[graph.ConversationId] = graph;
        }
    }
}
