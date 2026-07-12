using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.Progression;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.Terrain
{
    // Heals each faction's units standing on healing tiles (Fort/Throne/Gate/Village) when their phase starts.
    // Subscribes after UnitRegistry.ResetPhaseFlags via EventService — that ordering is intentional.
    public class HealingTileSystem : MonoBehaviour
    {
        [SerializeField] private TerrainStatTable terrainStatTable;
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private HealFloatSpawner healFloatSpawner;

        private void Awake()
        {
            EventService.Instance.SubscribePhaseStarted(OnPhaseStarted);
        }

        private void OnDestroy()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribePhaseStarted(OnPhaseStarted);
        }

        private void OnPhaseStarted(BattlePhase phase, int turnNumber)
        {
            if (TurnManager.Instance == null || terrainStatTable == null || mapRenderer == null)
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

            var terrain = mapRenderer.GetTerrainType(unit.gridPosition.x, unit.gridPosition.y);
            var stats = terrainStatTable.GetStats(terrain);
            if (stats.healPerTurn <= 0) return;

            int before = unit.UnitInstance.CurrentHP;
            unit.UnitInstance.ApplyHealing(stats.healPerTurn);
            int gained = unit.UnitInstance.CurrentHP - before;

            if (gained > 0)
                healFloatSpawner?.Show(unit.transform.position, gained);
        }
    }
}
