using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Lookup from a string map id to its MapData asset. One shared asset; lets the campaign flow
    // (or debug / future save data) request a battle by id, without holding a direct asset
    // reference. Each MapData declares its own id. Mirrors DialogueSpeakerRegistry's lazy index.
    [CreateAssetMenu(menuName = "Project Astra/Map/Map Catalog")]
    public class MapCatalog : ScriptableObject
    {
        [SerializeField] private List<MapData> _maps = new();

        private Dictionary<string, MapData> _byId;

        public MapData Get(string id)
        {
            EnsureIndexBuilt();
            return _byId.TryGetValue(id, out MapData map) ? map : null;
        }

        private void EnsureIndexBuilt()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, MapData>();
            foreach (MapData map in _maps)
                if (map != null && !string.IsNullOrEmpty(map.MapId))
                    _byId[map.MapId] = map;
        }
    }
}
