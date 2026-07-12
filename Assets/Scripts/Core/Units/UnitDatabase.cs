using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Units
{
    // Lookup from a MapData UnitStartPosition's unitId to its UnitDefinition asset. One shared
    // asset the UnitSpawner resolves authored ids through. Mirrors DialogueSpeakerRegistry.
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Database")]
    public class UnitDatabase : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> units = new();

        private Dictionary<string, UnitDefinition> byId;

        // The registered units, in author order. Editor tooling reads this to offer a picker of
        // the ids that will actually resolve at runtime.
        public IReadOnlyList<UnitDefinition> Units => units;

        public bool TryResolve(string unitId, out UnitDefinition definition)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(unitId ?? string.Empty, out definition);
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, UnitDefinition>();
            foreach (UnitDefinition unit in units)
                if (unit != null && !string.IsNullOrEmpty(unit.UnitId))
                    byId[unit.UnitId] = unit;
        }
    }
}
