using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Lookup from a string location id to its GurukulLocationData asset, so visits and doors can name a
    // room without holding a direct reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "GurukulLocationDatabase", menuName = "Project Astra/Gurukul/Location Database")]
    public class GurukulLocationDatabase : ScriptableObject
    {
        [SerializeField] private List<GurukulLocationData> locations = new();

        private Dictionary<string, GurukulLocationData> byId;

        public GurukulLocationData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulLocationData location) ? location : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulLocationData>();
            foreach (GurukulLocationData location in locations)
                if (location != null && !string.IsNullOrEmpty(location.LocationId))
                    byId[location.LocationId] = location;
        }
    }
}
