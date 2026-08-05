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

        // The campaign's map wins, starting the campaign at its first battle if something
        // skipped straight here. The serialized fallback is only for a scene with no GameFlow
        // alive at all.
        private MapData ResolveMap()
        {
            GameFlow flow = GameFlow.Instance;
            MapData campaignMap = flow != null ? flow.EnsureBattleStepStarted() : null;
            return campaignMap != null ? campaignMap : fallbackMapData;
        }
    }
}
