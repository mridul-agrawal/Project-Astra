using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Lookup from a visit id to its HubVisitData asset.
    [CreateAssetMenu(fileName = "HubVisitDatabase", menuName = "Project Astra/Hub/Visit Database")]
    public class HubVisitDatabase : ScriptableObject
    {
        [SerializeField] private List<HubVisitData> visits = new();

        private Dictionary<string, HubVisitData> byId;

        public HubVisitData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out HubVisitData visit) ? visit : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, HubVisitData>();
            foreach (HubVisitData visit in visits)
                if (visit != null && !string.IsNullOrEmpty(visit.VisitId))
                    byId[visit.VisitId] = visit;
        }
    }
}
