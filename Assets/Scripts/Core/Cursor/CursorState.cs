namespace ProjectAstra.Core.Cursor
{
    // The cursor's finite state, one step richer than CursorMode. CursorMode says what
    // input is allowed; CursorState says what the player is actually doing, which is what
    // the visual variants need to express. Moving is split out of CursorMode.Locked so a
    // walking unit reads differently from a suspended cursor.
    public enum CursorState
    {
        Free,
        Selected,
        Moving,
        ActionMenu,
        Targeting,
        Suspended
    }

    // What the cursor is sitting on while in Free. Hover is context on Free rather than a
    // set of substates — the visual layer only ever needs the kind, never a transition
    // between kinds.
    public enum CursorHover
    {
        Empty,
        ReadyAlly,
        ActedAlly,
        Enemy
    }

    // Why an input was refused. Carried on ErrorFeedback so a variant can react differently
    // to "that unit already went" than to "you can't walk there".
    public enum CursorErrorKind
    {
        InvalidTile,
        UnitAlreadyActed,
        NotYourUnit
    }
}
