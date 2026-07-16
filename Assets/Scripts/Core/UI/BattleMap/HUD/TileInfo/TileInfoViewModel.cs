using System.Text.RegularExpressions;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Controller for the Tile Info panel: watches the cursor, reads the terrain and
    // its bonuses, and pushes a TileInfoModel to the View. Holds all game coupling.
    // Owns its own phase visibility so the panel is independently disable-able.
    public sealed class TileInfoViewModel : MonoBehaviour
    {
        public TileInfoView View;          // wired by BattleMapHUDBuilder
        public TerrainStatTable StatTable; // wired by BattleMapHUDBuilder

        private GridCursor cursor;
        private MapRenderer map;

        private void Awake()
        {
            cursor = FindFirstObjectByType<GridCursor>();
            map    = FindFirstObjectByType<MapRenderer>();
            if (cursor != null) cursor.OnCursorMoved += HandleCursorMoved;
            EventService.Instance.SubscribePhaseStarted(HandlePhaseStarted);
        }

        private void Start()
        {
            var tm = TurnManager.Instance;
            ApplyPhase(tm != null ? tm.CurrentPhase : BattlePhase.PlayerPhase);
        }

        private void OnDestroy()
        {
            if (cursor != null) cursor.OnCursorMoved -= HandleCursorMoved;
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribePhaseStarted(HandlePhaseStarted);
        }

        private void HandleCursorMoved(Vector2Int pos)
        {
            // Per spec, tile info only updates during Player Phase.
            if (!IsPlayerPhase) return;
            if (View != null) View.Render(BuildModel(pos));
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber) => ApplyPhase(phase);

        private void ApplyPhase(BattlePhase phase)
        {
            if (phase == BattlePhase.PlayerPhase)
            {
                if (cursor != null) HandleCursorMoved(cursor.GridPosition);
            }
            else if (View != null)
            {
                View.SetVisible(false); // hidden during non-Player phases
            }
        }

        private TileInfoModel BuildModel(Vector2Int pos)
        {
            var terrain  = map != null ? map.GetTerrainType(pos.x, pos.y) : TerrainType.Plain;
            var unit     = FindUnitAt(pos);
            var moveType = unit != null ? unit.movementType : MovementType.Foot;

            int def = 0, avo = 0, heal = 0;
            if (StatTable != null)
            {
                var stats = StatTable.GetStats(terrain);
                (def, avo) = TerrainStatTable.GetTerrainBonuses(stats, moveType);
                heal = stats.healPerTurn;
            }

            return new TileInfoModel
            {
                Visible     = true,
                TerrainName = ToDisplayName(terrain),
                Defense     = def,
                Avoid       = avo,
                Heal        = heal,
                PanelOnLeft = ComputePanelOnLeft(pos),
            };
        }

        // FE GBA: panel goes to the side opposite the cursor.
        private bool ComputePanelOnLeft(Vector2Int cursorGridPos)
        {
            int mapWidth = (map != null && map.CurrentMap != null) ? map.CurrentMap.Width : 20;
            bool cursorOnLeft = cursorGridPos.x < mapWidth / 2;
            return !cursorOnLeft;
        }

        private bool IsPlayerPhase
        {
            get
            {
                var tm = TurnManager.Instance;
                return tm == null || tm.CurrentPhase == BattlePhase.PlayerPhase;
            }
        }

        // "TempleFloor" -> "Temple Floor", "Plain" -> "Plain"
        private static string ToDisplayName(TerrainType t)
            => Regex.Replace(t.ToString(), "(?<!^)([A-Z])", " $1");

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            var all = FindObjectsByType<TestUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gridPosition == pos) return all[i];
            return null;
        }
    }
}
