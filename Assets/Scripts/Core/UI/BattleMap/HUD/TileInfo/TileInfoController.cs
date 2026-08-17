using System.Text.RegularExpressions;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Turn;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Builds the tile info model from whatever terrain the cursor is standing on.
    //
    // §6 groups stat modifiers and flags into one strip in data order. Only chips that actually
    // say something are added, so a plain tile produces no strip at all and the name plate renders
    // on its own - which §6 calls the intended resting state for most of a map.
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

        // Public so a test or a capture can exercise the real derivation for a real tile, instead of
        // asserting a hand-built model and proving nothing about what the game would show.
        public TileInfoModel BuildModel(Vector2Int pos, HudCorner corner)
        {
            TerrainType terrainType = MapService.Instance.GetTerrainType(pos);
            TerrainStats terrainStats = MapService.Instance.GetStats(pos);

            var model = new TileInfoModel
            {
                TerrainName = BuildName(pos, terrainType),
                Corner = corner,
            };

            TileEffectStrip effects = BuildEffectStrip(terrainType, terrainStats);
            if (effects != null) model.Strips.Add(effects);

            return model;
        }

        // §6 composite names: the tile's own terrain, plus any prop standing on it.
        private static string BuildName(Vector2Int pos, TerrainType terrainType)
        {
            string name = ToDisplayName(terrainType);
            if (MapService.Instance.TryGetPropAt(pos, out string propId))
                name += " + " + ToDisplayName(propId);
            return name;
        }

        private static TileEffectStrip BuildEffectStrip(TerrainType terrainType, TerrainStats stats)
        {
            var strip = new TileEffectStrip();

            if (stats.defenceBonus != 0) strip.Chips.Add(TileEffectChip.Stat("Def", stats.defenceBonus));
            if (stats.avoidBonus != 0) strip.Chips.Add(TileEffectChip.Stat("Avo", stats.avoidBonus));
            if (stats.healPerTurn > 0) strip.Chips.Add(TileEffectChip.Stat("Heal/Turn", stats.healPerTurn));

            if (IsImpassable(stats)) strip.Chips.Add(TileEffectChip.Flag("Impassable"));
            if (terrainType == TerrainType.Wall) strip.Chips.Add(TileEffectChip.Flag("Unbreakable"));

            return strip.Chips.Count > 0 ? strip : null;
        }

        // A zero move cost already means "cannot enter" in the terrain table, so the flag reads it
        // straight off foot movement: if a foot soldier cannot stand here, the tile is impassable.
        // Foot is the yardstick because a flier crossing a river does not make the river walkable.
        private static bool IsImpassable(TerrainStats stats) => stats.moveCostFoot == 0;

        private bool IsPlayerPhase => TurnManager.Instance.CurrentPhase == BattlePhase.PlayerPhase;

        // "TempleFloor" -> "Temple Floor", "Plain" -> "Plain"
        private static string ToDisplayName(TerrainType t) => ToDisplayName(t.ToString());

        private static string ToDisplayName(string raw) =>
            Regex.Replace(raw, "(?<!^)([A-Z])", " $1");
    }
}
