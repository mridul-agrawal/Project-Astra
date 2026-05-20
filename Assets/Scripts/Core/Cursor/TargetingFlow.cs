using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Owns the Targeting mode — picking which enemy to attack or which ally
    // to heal. Holds the ordered cycle of target tiles and the current
    // cursor index into that list, drives the combat-forecast preview, and
    // toggles the range-highlighter between Attack and Heal palettes.
    //
    // Exposes pure queries (GetEnemiesInAttackRange / GetAlliesInHealRange)
    // that callers use BEFORE entering targeting — typically to decide
    // whether the action menu should even offer Attack / Heal.
    public class TargetingFlow
    {
        private readonly PathfindingService _pathfindingService;
        private readonly MapRenderer _mapRenderer;
        private readonly RangeHighlighter _rangeHighlighter;
        private readonly CombatForecastUI _combatForecastUI;
        private readonly GridCursor _cursor;

        private TestUnit _selectedUnit;
        private List<Vector2Int> _targetTiles;
        private int _targetIndex;
        private bool _isHealTargeting;

        public bool IsHealTargeting => _isHealTargeting;
        public List<Vector2Int> CurrentTargetTiles => _targetTiles;

        public TargetingFlow(
            PathfindingService pathfindingService,
            MapRenderer mapRenderer,
            RangeHighlighter rangeHighlighter,
            CombatForecastUI combatForecastUI,
            GridCursor cursor)
        {
            _pathfindingService = pathfindingService;
            _mapRenderer = mapRenderer;
            _rangeHighlighter = rangeHighlighter;
            _combatForecastUI = combatForecastUI;
            _cursor = cursor;
        }

        // --- Pure queries (no mode change) ---

        // Enemies the unit can attack from committedDestination, given its
        // weapon range.
        public List<Vector2Int> GetEnemiesInAttackRange(TestUnit selectedUnit, Vector2Int committedDestination)
        {
            var attackRange = _pathfindingService.ComputeAttackRange(
                new HashSet<Vector2Int> { committedDestination },
                selectedUnit.attackRangeMin, selectedUnit.attackRangeMax);

            var enemyTiles = new List<Vector2Int>();
            foreach (var tile in attackRange)
            {
                var unit = FindUnitAt(tile);
                if (unit != null && IsEnemy(unit))
                    enemyTiles.Add(tile);
            }
            return enemyTiles;
        }

        // Allies (non-enemy, not self) in staff range from committedDestination,
        // whose HP is below max. magStat scales staff range for Ranged/AoE staves.
        public List<Vector2Int> GetAlliesInHealRange(TestUnit selectedUnit, Vector2Int committedDestination, int magStat)
        {
            var staff = selectedUnit.equippedWeapon;
            var healRange = new HashSet<Vector2Int>();
            var map = _mapRenderer.CurrentMap;
            StaffRangeResolver.GetTargetTiles(staff, magStat, committedDestination, map.Width, map.Height, healRange);

            var allyTiles = new List<Vector2Int>();
            foreach (var tile in healRange)
            {
                var unit = FindUnitAt(tile);
                if (unit == null || unit == selectedUnit) continue;
                if (IsEnemy(unit)) continue;

                int hp = unit.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit.currentHP;
                int maxHP = unit.UnitInstance != null ? unit.UnitInstance.MaxHP : unit.maxHP;
                if (hp < maxHP)
                    allyTiles.Add(tile);
            }
            return allyTiles;
        }

        // --- Mode entry ---

        public void EnterAttackTargeting(TestUnit selectedUnit, List<Vector2Int> enemyTiles)
        {
            _selectedUnit = selectedUnit;
            _isHealTargeting = false;
            _targetTiles = SortedByGridPosition(enemyTiles);
            _targetIndex = 0;

            _rangeHighlighter?.ShowAttackRange(new HashSet<Vector2Int>(enemyTiles));

            _cursor.SetPosition(_targetTiles[0]);
            _cursor.SetMode(CursorMode.Targeting);

            UpdateForecastForCurrentTarget();
        }

        public void EnterHealTargeting(TestUnit selectedUnit, List<Vector2Int> healTiles)
        {
            _selectedUnit = selectedUnit;
            _isHealTargeting = true;
            _targetTiles = SortedByGridPosition(healTiles);
            _targetIndex = 0;

            _rangeHighlighter?.ShowHealRange(new HashSet<Vector2Int>(healTiles));

            _cursor.SetPosition(_targetTiles[0]);
            _cursor.SetMode(CursorMode.Targeting);

            UpdateForecastForCurrentTarget();
        }

        // --- Active-targeting input ---

        // Step to the next/previous target in the cycle. Direction's sign
        // picks forward or backward; the magnitude doesn't matter.
        public void Cycle(Vector2Int direction)
        {
            if (_targetTiles == null || _targetTiles.Count == 0) return;

            int step = (direction.x > 0 || direction.y > 0) ? 1 : -1;
            _targetIndex = (_targetIndex + step + _targetTiles.Count) % _targetTiles.Count;

            _cursor.SetPosition(_targetTiles[_targetIndex]);
            UpdateForecastForCurrentTarget();
        }

        // Cancels targeting and clears the highlighter. Doesn't drive what
        // happens next — the caller decides (typically: return to the action
        // menu at the committed destination).
        public void Cancel()
        {
            _isHealTargeting = false;
            _targetTiles = null;
            _selectedUnit = null;
            _rangeHighlighter?.ClearAll();
        }

        // Used by Cancel/cleanup paths outside the targeting flow itself
        // (e.g. unit deselect) — same as Cancel but kept separate so a
        // caller that only wants to clear targeting state without touching
        // the highlighter can choose.
        public void ClearState()
        {
            _isHealTargeting = false;
            _targetTiles = null;
            _selectedUnit = null;
        }

        // --- Forecast + helpers ---

        public void UpdateForecastForCurrentTarget()
        {
            if (_combatForecastUI == null || _selectedUnit == null) return;
            if (_cursor.CurrentMode != CursorMode.Targeting) { _combatForecastUI.Hide(); return; }

            var target = FindUnitAt(_cursor.GridPosition);
            if (target == null) { _combatForecastUI.Hide(); return; }

            int distance = Mathf.Abs(_selectedUnit.gridPosition.x - target.gridPosition.x)
                         + Mathf.Abs(_selectedUnit.gridPosition.y - target.gridPosition.y);

            if (_isHealTargeting)
                _combatForecastUI.ShowStaffHeal(_selectedUnit, target);
            else
                _combatForecastUI.ShowCombat(_selectedUnit, target, distance);
        }

        private static List<Vector2Int> SortedByGridPosition(List<Vector2Int> source)
        {
            var copy = new List<Vector2Int>(source);
            copy.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return copy;
        }

        // Prefer the TurnManager-registered faction when available; otherwise
        // fall back to the unit's own field. A unit that's registered but has
        // no faction entry counts as not-enemy (matches the original
        // GetFaction(unit) == Faction.Enemy comparison with a null-valued
        // nullable Faction returning false).
        private static bool IsEnemy(TestUnit unit)
        {
            return TurnManager.Instance != null
                ? TurnManager.Instance.UnitRegistry.GetFaction(unit) == Faction.Enemy
                : unit.faction == Faction.Enemy;
        }

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == pos)
                    return unit;
            return null;
        }
    }
}
