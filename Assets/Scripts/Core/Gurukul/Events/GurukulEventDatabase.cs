using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul.Events
{
    // Lookup from an event id to its asset, and the list the area-trigger watcher walks. Mirrors
    // MapCatalog.
    [CreateAssetMenu(fileName = "GurukulEventDatabase", menuName = "Project Astra/Gurukul/Event Database")]
    public class GurukulEventDatabase : ScriptableObject
    {
        [SerializeField] private List<GurukulEventData> events = new();

        private Dictionary<string, GurukulEventData> byId;

        public IReadOnlyList<GurukulEventData> All => events;

        public GurukulEventData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulEventData found) ? found : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulEventData>();
            foreach (GurukulEventData authored in events)
                if (authored != null && !string.IsNullOrEmpty(authored.EventId))
                    byId[authored.EventId] = authored;
        }
    }
}
