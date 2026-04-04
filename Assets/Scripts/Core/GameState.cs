namespace ProjectAstra.Core
{
    /// <summary>All top-level game states — only one active at a time, transitions validated by GameStateTransitionTable.</summary>
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
        ChapterClear,
        GameOver,
        SaveMenu,
        SettingsMenu
    }
}
