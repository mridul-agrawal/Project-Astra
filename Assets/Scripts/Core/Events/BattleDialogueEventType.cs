namespace ProjectAstra.Core.Events
{
    // Battle-map moments other systems react to. PlayerPhaseStarted comes from the turn
    // system; the others are raised by the cursor as the player acts.
    public enum BattleDialogueEventType
    {
        PlayerPhaseStarted,
        UnitSelected,
        MoveConfirmed,
        PreCombat
    }
}
