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
        [SerializeField] private List<MapData> maps = new();

        private Dictionary<string, MapData> byId;

        public MapData Get(string id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out MapData map) ? map : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<string, MapData>();
            foreach (MapData map in maps)
                if (map != null && !string.IsNullOrEmpty(map.MapId))
                    byId[map.MapId] = map;
        }
    }
}
