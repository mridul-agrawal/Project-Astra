namespace ProjectAstra.Core.Grid
{
    // Every distinct terrain a map tile can be. Drives movement cost, combat bonuses,
    // and special effects (heal, capture). Save data references these by integer index
    // — don't reorder; append new entries at the end only.
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
