using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Grid
{
    // Renders the map the campaign is on, then spawns its units. Runs early so units exist before TurnManager registers them.
    [DefaultExecutionOrder(-100)]
    public class MapBootstrapper : MonoBehaviour
    {
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private UnitSpawner unitSpawner;
        [SerializeField] private TerrainStatTable terrainStatTable;
        [Tooltip("Fallback map for pressing Play directly in this scene (when GameFlow isn't running).")]
        [SerializeField] private MapData fallbackMapData;

        private void Start()
        {
            MapData map = ResolveMap();
            if (mapRenderer == null || map == null) return;

            MapService.Load(map, terrainStatTable);
            mapRenderer.LoadMap(map);
            if (unitSpawner != null)
                unitSpawner.SpawnUnits(map);
        }

        // The campaign's current map wins; otherwise use the serialized editor fallback.
        private MapData ResolveMap()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow != null && flow.CurrentMap != null)
                return flow.CurrentMap;
            return fallbackMapData;
        }
    }
}
