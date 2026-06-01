using System;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Resolves a single combat round and hands the deterministic result off
    // to the CombatPlaybackDispatcher for animation (Skip / Fast / Normal).
    // The dispatcher's chosen controller writes HP per-hit, plays the chosen
    // visuals, and at the end invokes the onComplete callback the caller
    // passed in — control returns to the caller (today, GridCursor) only
    // after the animation finishes.
    //
    // No grid-cursor or input dependency — composable from any system that
    // wants to fire a combat round.
    public class CombatExecutor
    {
        private readonly MapRenderer _mapRenderer;
        private readonly TerrainStatTable _terrainStatTable;
        private readonly UnitDeathEventChannel _deathChannel;
        private readonly CombatForecastUI _combatForecastUI;
        private readonly ToastNotificationUI _toastUI;
        private readonly CombatPlaybackDispatcher _dispatcher;

        public CombatExecutor(
            MapRenderer mapRenderer,
            TerrainStatTable terrainStatTable,
            UnitDeathEventChannel deathChannel,
            CombatForecastUI combatForecastUI,
            ToastNotificationUI toastUI,
            CombatPlaybackDispatcher dispatcher)
        {
            _mapRenderer = mapRenderer;
            _terrainStatTable = terrainStatTable;
            _deathChannel = deathChannel;
            _combatForecastUI = combatForecastUI;
            _toastUI = toastUI;
            _dispatcher = dispatcher;
        }

        // Fires one combat round and dispatches the result for playback.
        // onComplete is invoked when playback finishes — early-outs (no
        // defender, attacker unarmed) call it synchronously so the caller
        // never has to branch on the path taken.
        public void TryCommitAttack(TestUnit attacker, TestUnit defender, Action onComplete)
        {
            _combatForecastUI?.Hide();

            if (defender == null) { onComplete?.Invoke(); return; }

            if (attacker.Inventory.IsUnarmed)
            {
                Debug.LogWarning($"[Combat] {attacker.name} is unarmed; cannot attack.");
                onComplete?.Invoke();
                return;
            }

            var result = ResolveCombat(attacker, defender);
            LogCombatResult(attacker, defender, result);

            var ctx = new CombatPlaybackContext
            {
                Attacker = attacker,
                Defender = defender,
                Result = result,
                DeathChannel = _deathChannel,
                ToastUI = _toastUI,
                OnComplete = onComplete,
                DefenderTerrain = _mapRenderer != null
                    ? _mapRenderer.GetTerrainType(defender.gridPosition.x, defender.gridPosition.y)
                    : default,
            };

            if (_dispatcher != null)
            {
                _dispatcher.Dispatch(ctx);
            }
            else
            {
                // Safety net: no dispatcher wired (test seam / misconfigured
                // scene). Apply terminal result instantly, fire callback.
                CombatResultApplicator.Finalize(ctx);
                onComplete?.Invoke();
            }
        }

        // --- Combat resolution ---

        private CombatResult ResolveCombat(TestUnit attacker, TestUnit defender)
        {
            int distance = Mathf.Abs(attacker.gridPosition.x - defender.gridPosition.x)
                         + Mathf.Abs(attacker.gridPosition.y - defender.gridPosition.y);

            var atkData = BuildCombatantData(attacker, distance);
            var defData = BuildCombatantData(defender, distance);

            var (defTerrainDef, defTerrainAvo) = GetTerrainBonuses(defender);
            var (atkTerrainDef, atkTerrainAvo) = GetTerrainBonuses(attacker);

            var atkClass = attacker.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            var defClass = defender.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;

            return CombatRound.Resolve(atkData, defData, defTerrainDef, defTerrainAvo,
                atkTerrainDef, atkTerrainAvo, new UnityRng(), atkClass, defClass);
        }

        private static CombatantData BuildCombatantData(TestUnit unit, int distance)
        {
            if (unit.UnitInstance != null)
            {
                int classCrit = unit.UnitInstance.CurrentClass != null ? unit.UnitInstance.CurrentClass.CritBonus : 0;
                return CombatantData.FromStats(unit.UnitInstance.Stats,
                    unit.UnitInstance.CurrentHP, unit.UnitInstance.MaxHP,
                    unit.equippedWeapon, distance, classCrit);
            }

            // Test-seam fallback for legacy TestUnits without a UnitInstance.
            var stats = StatArray.From(unit.maxHP, 8, 3, 7, 9, 5, 2, 6, 5);
            return CombatantData.FromStats(stats, unit.currentHP, unit.maxHP, unit.equippedWeapon, distance);
        }

        private (int def, int avo) GetTerrainBonuses(TestUnit unit)
        {
            if (_mapRenderer == null || _terrainStatTable == null) return (0, 0);

            var terrain = _mapRenderer.GetTerrainType(unit.gridPosition.x, unit.gridPosition.y);
            var stats = _terrainStatTable.GetStats(terrain);
            return TerrainStatTable.GetTerrainBonuses(stats, unit.movementType);
        }

        // --- Logging ---

        private static void LogCombatResult(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            foreach (var hit in result.Hits)
            {
                string target = hit.Attacker == "Attacker" ? defender.name : attacker.name;
                string source = hit.Attacker == "Attacker" ? attacker.name : defender.name;

                if (hit.Hit)
                {
                    string critText = hit.Crit ? " CRITICAL!" : "";
                    Debug.Log($"[Combat] {source} → {target}: {hit.Damage} damage{critText}");
                }
                else
                {
                    Debug.Log($"[Combat] {source} → {target}: MISS");
                }
            }

            if (result.TriangleAdvantage != 0)
            {
                string side = result.TriangleAdvantage > 0 ? "Attacker advantage" : "Defender advantage";
                Debug.Log($"[Combat] Weapon Triangle: {side}");
            }
            if (result.AttackerEffective)
                Debug.Log($"[Combat] Effective weapon!");

            Debug.Log($"[Combat] Result: {attacker.name} HP={result.AttackerHPAfter}, {defender.name} HP={result.DefenderHPAfter}");
        }
    }
}
