namespace ProjectAstra.Core.Grid
{
    // Stable identifier for every battle map — the selection vocabulary used across the game
    // (campaign flow, debug, future save data). Resolves to a MapData asset via MapCatalog.
    // Add a member when you add a map; never reorder/renumber existing ones.
    public enum MapId
    {
        None = 0,
        Map1_BridgeAtSuvarnapur,
    }
}
