namespace ProjectAstra.Core.UI
{
    // UI-02. The screen / flow that opened the Unit Info Panel.
    // Determines interactivity (e.g., inventory is reorderable only in PreBattlePrep),
    // display state (ObituaryFromSupportList freezes everything at death state),
    // and which context to return to on Cancel.
    public enum UnitInfoContext
    {
        BattleMap = 0,
        PauseMenu = 1,
        PreBattlePrep = 2,
        Camp = 3,
        Deployment = 4,
        ObituaryFromSupportList = 5,
    }
}
