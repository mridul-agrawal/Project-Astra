using System;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Runs a single combat round on behalf of the attacker against a chosen
    // defender. Wraps CombatRound.Resolve with the surrounding ceremony:
    // forecast hide, HP/EXP application, death routing, item-break toasts,
    // and a completion callback that hands control back to the caller.
    //
    // No grid-cursor or input dependency — composable from any system that
    // wants to fire a combat round (today, GridCursor at HandleConfirm).
    public class CombatExecutor
    {
        private readonly MapRenderer _mapRenderer;
        private readonly TerrainStatTable _terrainStatTable;
        private readonly UnitDeathEventChannel _deathChannel;
        private readonly CombatForecastUI _combatForecastUI;
        private readonly ToastNotificationUI _toastUI;

        public CombatExecutor(
            MapRenderer mapRenderer,
            TerrainStatTable terrainStatTable,
            UnitDeathEventChannel deathChannel,
            CombatForecastUI combatForecastUI,
            ToastNotificationUI toastUI)
        {
            _mapRenderer = mapRenderer;
            _terrainStatTable = terrainStatTable;
            _deathChannel = deathChannel;
            _combatForecastUI = combatForecastUI;
            _toastUI = toastUI;
        }

        // Fires one combat round and applies its consequences. Invokes
        // onComplete when finished — early-outs (no defender, attacker
        // unarmed) call it too, so the caller never has to branch.
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
            ApplyCombatResult(attacker, defender, result);
            ApplyDurability(attacker, defender, result);

            onComplete?.Invoke();
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

        // --- Logging + result application ---

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

        private void ApplyCombatResult(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            SetUnitHP(attacker, result.AttackerHPAfter);
            SetUnitHP(defender, result.DefenderHPAfter);

            if (result.DefenderDied) HandleUnitDeathAfterCombat(defender, attacker);
            if (result.AttackerDied) HandleUnitDeathAfterCombat(attacker, defender);

            GrantCombatExp(attacker, defender, result);
        }

        private static void SetUnitHP(TestUnit unit, int hp)
        {
            if (unit.UnitInstance != null)
                unit.UnitInstance.SetCurrentHP(hp);
            else
                unit.currentHP = hp;
        }

        private void HandleUnitDeathAfterCombat(TestUnit died, TestUnit killer)
        {
            UnitDeathHook.HandleDeath(died, killer, _deathChannel);
            if (died.isLord) Convoy.Current = NullConvoy.Instance;
        }

        private static void GrantCombatExp(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            if (ExpGranter.Instance == null) return;
            if (attacker.UnitInstance == null || defender.UnitInstance == null) return;

            if (result.AttackerFired && !result.AttackerDied)
            {
                int exp = ExpMath.ComputeCombatExp(
                    attacker.UnitInstance, defender.UnitInstance, result.DefenderDied);
                ExpGranter.Instance.Grant(attacker, exp);
            }

            if (result.DefenderFired && !result.DefenderDied)
            {
                int exp = ExpMath.ComputeCombatExp(
                    defender.UnitInstance, attacker.UnitInstance, result.AttackerDied);
                ExpGranter.Instance.Grant(defender, exp);
            }
        }

        private void ApplyDurability(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            if (result.AttackerFired)
                ItemBreakToaster.WithBreakAnnouncements(attacker, _toastUI,
                    () => attacker.Inventory.ConsumeEquippedWeaponUses(1));
            if (result.DefenderFired)
                ItemBreakToaster.WithBreakAnnouncements(defender, _toastUI,
                    () => defender.Inventory.ConsumeEquippedWeaponUses(1));
        }
    }
}
