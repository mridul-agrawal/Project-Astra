using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub.Events
{
    // Lookup from an event id to its asset, and the list the area-trigger watcher walks.
    [CreateAssetMenu(fileName = "HubEventDatabase", menuName = "Project Astra/Hub/Event Database")]
    public class HubEventDatabase : ScriptableObject
    {
        [SerializeField] private List<HubEventData> events = new();

        private Dictionary<string, HubEventData> byId;

        public IReadOnlyList<HubEventData> All => events;

        public HubEventData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out HubEventData found) ? found : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, HubEventData>();
            foreach (HubEventData authored in events)
                if (authored != null && !string.IsNullOrEmpty(authored.EventId))
                    byId[authored.EventId] = authored;
        }
    }
}
