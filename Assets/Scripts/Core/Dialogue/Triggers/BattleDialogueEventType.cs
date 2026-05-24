namespace ProjectAstra.Core.Dialogue
{
    // Battle-map moments a tutorial dialogue can hook onto. PlayerPhaseStarted comes
    // from the turn system; the others are raised by the cursor as the player acts.
    public enum BattleDialogueEventType
    {
        PlayerPhaseStarted,
        UnitSelected,
        MoveConfirmed,
        PreCombat
    }
}
