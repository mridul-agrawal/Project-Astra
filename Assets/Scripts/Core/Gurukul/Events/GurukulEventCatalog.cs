using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul.Events
{
    // Lookup from an event id to its asset, and the list the area-trigger watcher walks. Mirrors
    // MapCatalog.
    [CreateAssetMenu(fileName = "GurukulEventCatalog", menuName = "Project Astra/Gurukul/Event Catalog")]
    public class GurukulEventCatalog : ScriptableObject
    {
        [SerializeField] private List<GurukulEvent> events = new();

        private Dictionary<string, GurukulEvent> byId;

        public IReadOnlyList<GurukulEvent> All => events;

        public GurukulEvent Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulEvent found) ? found : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulEvent>();
            foreach (GurukulEvent authored in events)
                if (authored != null && !string.IsNullOrEmpty(authored.EventId))
                    byId[authored.EventId] = authored;
        }
    }
}
