namespace ProjectAstra.Core.Cursor
{
    // Operational modes for the grid cursor — drives movement constraints and
    // input response. Pure transient runtime state, not serialized anywhere.
    public enum CursorMode
    {
        Free,           // Browse the map freely within bounds.
        UnitSelected,   // Constrained to the unit's reachable tiles.
        ActionMenu,     // Unit has moved; choosing an action (Attack / Wait / …).
        Targeting,      // Constrained to valid attack or heal target tiles.
        Locked          // No movement, no input (enemy phase, animations, menus).
    }
}
