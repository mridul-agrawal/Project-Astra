namespace ProjectAstra.Core
{
    /// <summary>
    /// Classification for how a unit traverses terrain. Maps 1:1 to the moveCost fields
    /// in TerrainStats — each unit has exactly one movement type that determines which
    /// cost column is used for pathfinding.
    /// </summary>
    public enum MovementType
    {
        Foot,
        Mounted,
        Armoured,
        Flying,
        Pirate
    }
}
