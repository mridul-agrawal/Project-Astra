using Codice.Client.BaseCommands;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public class TileInfoController
    {
        public TileInfoView tileInfoView;
        public TileInfoModel tileInfoModel;

        public TileInfoController(TileInfoView tileInfoView)
        {
            this.tileInfoView = tileInfoView;
            this.tileInfoModel = new TileInfoModel();
        }

        public void HandleCursorMoved(Vector2Int pos, HudCorner corner) => UpdateTileInfoView(pos, corner);

        public void HandlePhaseStarted(BattlePhase phase, Vector2Int pos, HudCorner corner) => UpdateTileInfoView(pos, corner);

        private void UpdateTileInfoView(Vector2Int pos, HudCorner corner)
        {
            if (!IsPlayerPhase) return;

            tileInfoModel = BuildModel(pos, corner);
            if (tileInfoView != null)
                tileInfoView.Render(tileInfoModel);
        }

        private TileInfoModel BuildModel(Vector2Int pos, HudCorner corner)
        {
            TerrainType terrainType = MapService.Instance.GetTerrainType(pos);
            TerrainStats terrainStats = MapService.Instance.GetStats(pos);

            return new TileInfoModel
            {
                TerrainName = ToDisplayName(terrainType),
                Defense = terrainStats.defenceBonus,
                Avoid = terrainStats.avoidBonus,
                Heal = terrainStats.healPerTurn,
                Corner = corner
            };
        }

        private bool IsPlayerPhase
        {
            get
            {
                return TurnManager.Instance.CurrentPhase == BattlePhase.PlayerPhase;
            }
        }

        // "TempleFloor" -> "Temple Floor", "Plain" -> "Plain"
        private static string ToDisplayName(TerrainType t) => Regex.Replace(t.ToString(), "(?<!^)([A-Z])", " $1");
    }
}
