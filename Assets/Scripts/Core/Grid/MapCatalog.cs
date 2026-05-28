using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Lookup from a MapId to its MapData asset. One shared asset; lets the campaign flow (or
    // debug / future save data) request a battle by enum, without holding a direct asset
    // reference. Each MapData declares its own Id. Mirrors DialogueSpeakerRegistry's lazy index.
    [CreateAssetMenu(menuName = "Project Astra/Map/Map Catalog")]
    public class MapCatalog : ScriptableObject
    {
        [SerializeField] private List<MapData> _maps = new();

        private Dictionary<MapId, MapData> _byId;

        public MapData Get(MapId id)
        {
            EnsureIndexBuilt();
            return _byId.TryGetValue(id, out MapData map) ? map : null;
        }

        private void EnsureIndexBuilt()
        {
            if (_byId != null) return;
            _byId = new Dictionary<MapId, MapData>();
            foreach (MapData map in _maps)
                if (map != null && map.Id != MapId.None)
                    _byId[map.Id] = map;
        }
    }
}
