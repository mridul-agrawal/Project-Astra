using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Grid
{
    // Renders the map the campaign is on, then spawns its units. The map comes from GameFlow
    // (the campaign director) when a game is running; otherwise it falls back to the serialized
    // _mapData so the scene still plays directly in the editor. Runs early so units exist
    // before TurnManager registers them.
    [DefaultExecutionOrder(-100)]
    public class MapBootstrapper : MonoBehaviour
    {
        [SerializeField] private MapRenderer _mapRenderer;
        [SerializeField] private UnitSpawner _unitSpawner;
        [Tooltip("Fallback map for pressing Play directly in this scene (when GameFlow isn't running).")]
        [SerializeField] private MapData _mapData;

        private void Start()
        {
            MapData map = ResolveMap();
            if (_mapRenderer == null || map == null) return;

            _mapRenderer.LoadMap(map);
            if (_unitSpawner != null)
                _unitSpawner.SpawnUnits(map);
        }

        // The campaign's current map wins; otherwise use the serialized editor fallback.
        private MapData ResolveMap()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow != null && flow.CurrentMap != null)
                return flow.CurrentMap;
            return _mapData;
        }
    }
}
