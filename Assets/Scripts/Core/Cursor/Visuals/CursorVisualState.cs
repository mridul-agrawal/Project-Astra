namespace ProjectAstra.Core.Cursor
{
    // The five looks a variant has to express, collapsed from (CursorState, CursorHover).
    // Everything past Selected — moving, action menu, targeting — keeps the Selected look,
    // because from the player's side the unit is still picked up.
    public enum CursorVisualState
    {
        Idle,
        Selectable,
        Selected,
        Acted,
        Enemy
    }

    public static class CursorVisualStateMap
    {
        public static CursorVisualState From(CursorState state, CursorHover hover)
        {
            if (state != CursorState.Free) return CursorVisualState.Selected;

            return hover switch
            {
                CursorHover.ReadyAlly => CursorVisualState.Selectable,
                CursorHover.ActedAlly => CursorVisualState.Acted,
                CursorHover.Enemy => CursorVisualState.Enemy,
                _ => CursorVisualState.Idle,
            };
        }
    }
}
