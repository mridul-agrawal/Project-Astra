namespace ProjectAstra.Core.Pathfinding
{
    // How a unit traverses terrain. Each value maps 1:1 to a moveCost column in
    // TerrainStats. Stored on class assets as an integer, so adding new entries
    // is fine but reordering will silently rewire which terrain costs every
    // existing class uses.
    public enum MovementType
    {
        Foot,
        Mounted,
        Armoured,
        Flying,
        Pirate,
        Thief
    }
}
