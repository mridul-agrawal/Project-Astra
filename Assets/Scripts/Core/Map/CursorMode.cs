namespace ProjectAstra.Core
{
    /// <summary>
    /// Operational modes for the grid cursor, controlling movement constraints and input response.
    /// </summary>
    public enum CursorMode
    {
        Free,           // Browse map freely within bounds
        UnitSelected,   // Constrained to unit's reachable tiles
        Targeting,      // Constrained to valid attack target tiles
        Locked          // No movement, no input (enemy phase, animations, menus)
    }
}
