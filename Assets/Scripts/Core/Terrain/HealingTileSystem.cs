using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.Progression;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.Terrain
{
    // Heals each faction's units standing on healing tiles (Fort/Throne/Gate/Village) when their phase starts.
    // Subscribes after UnitRegistry.ResetPhaseFlags via TurnEventChannel — that ordering is intentional.
    public class HealingTileSystem : MonoBehaviour
    {
        [SerializeField] private TurnEventChannel _turnEventChannel;
        [SerializeField] private TerrainStatTable _terrainStatTable;
        [SerializeField] private MapRenderer _mapRenderer;
        [SerializeField] private HealFloatSpawner _healFloatSpawner;

        private void Awake()
        {
            if (_turnEventChannel != null)
                _turnEventChannel.RegisterPhaseStarted(OnPhaseStarted);
        }

        private void OnDestroy()
        {
            if (_turnEventChannel != null)
                _turnEventChannel.UnregisterPhaseStarted(OnPhaseStarted);
        }

        private void OnPhaseStarted(BattlePhase phase, int turnNumber)
        {
            if (TurnManager.Instance == null || _terrainStatTable == null || _mapRenderer == null)
                return;

            var faction = TurnManager.PhaseToFaction(phase);
            var units = TurnManager.Instance.UnitRegistry.GetUnitsForFaction(faction);
            if (units == null) return;

            foreach (var unit in units)
                HealUnitIfStanding(unit);
        }

        private void HealUnitIfStanding(TestUnit unit)
        {
            if (unit == null || unit.UnitInstance == null) return;

            var terrain = _mapRenderer.GetTerrainType(unit.gridPosition.x, unit.gridPosition.y);
            var stats = _terrainStatTable.GetStats(terrain);
            if (stats.healPerTurn <= 0) return;

            int before = unit.UnitInstance.CurrentHP;
            unit.UnitInstance.ApplyHealing(stats.healPerTurn);
            int gained = unit.UnitInstance.CurrentHP - before;

            if (gained > 0)
                _healFloatSpawner?.Show(unit.transform.position, gained);
        }
    }
}
