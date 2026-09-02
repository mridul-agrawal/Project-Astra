using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Lookup from a location id to its HubLocationData asset.
    [CreateAssetMenu(fileName = "HubLocationDatabase", menuName = "Project Astra/Hub/Location Database")]
    public class HubLocationDatabase : ScriptableObject
    {
        [SerializeField] private List<HubLocationData> locations = new();

        private Dictionary<string, HubLocationData> byId;

        public HubLocationData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out HubLocationData location) ? location : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, HubLocationData>();
            foreach (HubLocationData location in locations)
                if (location != null && !string.IsNullOrEmpty(location.LocationId))
                    byId[location.LocationId] = location;
        }
    }
}
