using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Lookup from a string location id to its GurukulLocation asset, so visits and doors can name a
    // room without holding a direct reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "GurukulLocationCatalog", menuName = "Project Astra/Gurukul/Location Catalog")]
    public class GurukulLocationCatalog : ScriptableObject
    {
        [SerializeField] private List<GurukulLocation> locations = new();

        private Dictionary<string, GurukulLocation> byId;

        public GurukulLocation Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulLocation location) ? location : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulLocation>();
            foreach (GurukulLocation location in locations)
                if (location != null && !string.IsNullOrEmpty(location.LocationId))
                    byId[location.LocationId] = location;
        }
    }
}
