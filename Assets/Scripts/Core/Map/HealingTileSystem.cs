using ProjectAstra.Core.UI;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Phase-start healing: any unit of the current-phase faction standing on a
    /// tile with healPerTurn > 0 (Fort, Throne, Gate, Village) recovers HP,
    /// capped at MaxHP. Silent when the unit is already at max — no float shown.
    ///
    /// Subscribes to TurnEventChannel.RegisterPhaseStarted, which fires AFTER
    /// UnitRegistry.ResetPhaseFlags inside TurnManager.BeginPhase — the correct
    /// canonical seam. Deferred: poison tick should subscribe to the same event
    /// and register BEFORE this component so damage resolves before healing.
    /// </summary>
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

            var faction = PhaseToFaction(phase);
            var units = TurnManager.Instance.UnitRegistry.GetUnitsForFaction(faction);
            if (units == null) return;

            foreach (var unit in units)
            {
                if (unit == null || unit.UnitInstance == null) continue;

                var terrain = _mapRenderer.GetTerrainType(unit.gridPosition.x, unit.gridPosition.y);
                var stats = _terrainStatTable.GetStats(terrain);
                if (stats.healPerTurn <= 0) continue;

                int before = unit.UnitInstance.CurrentHP;
                unit.UnitInstance.ApplyHealing(stats.healPerTurn);
                int gained = unit.UnitInstance.CurrentHP - before;

                if (gained > 0)
                    _healFloatSpawner?.Show(unit.transform.position, gained);
            }
        }

        private static Faction PhaseToFaction(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.PlayerPhase: return Faction.Player;
                case BattlePhase.EnemyPhase:  return Faction.Enemy;
                case BattlePhase.AlliedPhase: return Faction.Allied;
                default:                      return Faction.Player;
            }
        }
    }
}
