namespace ProjectAstra.Core.State
{
    // Every distinct top-level state the game can be in. Exactly one is active at a time;
    // moves between them are gated by GameStateTransitionTable.
    public enum GameState
    {
        TitleScreen,
        MainMenu,
        Cutscene,
        PreBattlePrep,
        BattleMap,
        BattleMapPaused,
        CombatAnimation,
        Dialogue,
        WarLedger,
        ChapterClear,
        GameOver,
        SaveMenu,
        SettingsMenu,
        LevelUpScreen
    }
}
