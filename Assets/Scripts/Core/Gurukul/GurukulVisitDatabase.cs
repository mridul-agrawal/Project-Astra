using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Lookup from a string visit id to its GurukulVisit asset, so a campaign step can name a visit
    // without holding a direct asset reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "GurukulVisitCatalog", menuName = "Project Astra/Gurukul/Visit Catalog")]
    public class GurukulVisitCatalog : ScriptableObject
    {
        [SerializeField] private List<GurukulVisit> visits = new();

        private Dictionary<string, GurukulVisit> byId;

        public GurukulVisit Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulVisit visit) ? visit : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulVisit>();
            foreach (GurukulVisit visit in visits)
                if (visit != null && !string.IsNullOrEmpty(visit.VisitId))
                    byId[visit.VisitId] = visit;
        }
    }
}
