using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Lookup from a string visit id to its GurukulVisitData asset, so a campaign step can name a visit
    // without holding a direct asset reference. Mirrors MapCatalog.
    [CreateAssetMenu(fileName = "GurukulVisitDatabase", menuName = "Project Astra/Gurukul/Visit Database")]
    public class GurukulVisitDatabase : ScriptableObject
    {
        [SerializeField] private List<GurukulVisitData> visits = new();

        private Dictionary<string, GurukulVisitData> byId;

        public GurukulVisitData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out GurukulVisitData visit) ? visit : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, GurukulVisitData>();
            foreach (GurukulVisitData visit in visits)
                if (visit != null && !string.IsNullOrEmpty(visit.VisitId))
                    byId[visit.VisitId] = visit;
        }
    }
}
