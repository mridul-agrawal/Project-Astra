namespace ProjectAstra.Core.Grid
{
    // Every distinct terrain a map tile can be. Drives movement cost, combat bonuses,
    // and special effects (heal, capture). Don't reorder — add new terrains only at
    // the end. Unity stores these as integers in tileset and terrain-stat assets, so
    // reordering silently corrupts existing terrain data.
    public enum TerrainType
    {
        Plain,
        Forest,
        Mountain,
        Peak,
        Water,
        Sea,
        River,
        Road,
        Village,
        Fort,
        Gate,
        Chest,
        Door,
        Wall,
        DestructibleWall,
        Rubble,
        Sand,
        Void,
        Throne
    }
}
