using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Units
{
    // Lookup from a MapData UnitStartPosition's unitId to its UnitDefinition asset. One shared
    // asset the UnitSpawner resolves authored ids through. Mirrors DialogueSpeakerRegistry.
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Database")]
    public class UnitDatabase : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> _units = new();

        private Dictionary<string, UnitDefinition> _byId;

        public bool TryResolve(string unitId, out UnitDefinition definition)
        {
            EnsureIndexBuilt();
            return _byId.TryGetValue(unitId ?? string.Empty, out definition);
        }

        private void EnsureIndexBuilt()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, UnitDefinition>();
            foreach (UnitDefinition unit in _units)
                if (unit != null && !string.IsNullOrEmpty(unit.UnitId))
                    _byId[unit.UnitId] = unit;
        }
    }
}
