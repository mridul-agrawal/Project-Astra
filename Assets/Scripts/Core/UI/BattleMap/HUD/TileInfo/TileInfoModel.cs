namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Display data for the Tile Info panel: the terrain under the cursor and its
    // bonuses, plus which side of the screen the panel should sit on (opposite the
    // cursor, so it never covers the tile being inspected).
    public sealed class TileInfoModel
    {
        public bool Visible;
        public string TerrainName;
        public int Defense;
        public int Avoid;
        public int Heal;
        public bool PanelOnLeft;
    }
}
