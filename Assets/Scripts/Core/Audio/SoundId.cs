namespace ProjectAstra.Core.Audio
{
    // Unity stores these as integers in the AudioLibrary asset. Don't reorder — add new ids at the end.
    public enum SoundId
    {
        None = 0,

        // UI
        UiConfirm,
        UiCancel,
        UiMove,

        // Combat
        HitPhysical,
        HitCrit,
        Miss,

        // Music
        MusicMap,
        MusicBattle,

        // Ambient
        AmbientWind,

        // Cinematic / creatures
        RakshasaRoar,
        VillagerScream,

        // UI — context-specific confirms
        ConfirmStartGame,
        ConfirmStartBattle,
        ConfirmUnitSelect,
        ConfirmMove,
        ConfirmAction,
        ConfirmEngage,
        ConfirmItem,

        // UI — cancel + shared feedback
        CancelGrid,
        UiInvalid,
        UiPanelOpen,
        UiPanelClose,
        UiTab,
        UiHover,

        // Combat / gameplay
        AttackSwing,
        MagicCast,
        UnitDeath,
        Heal,
        Footstep,

        // Progression / items
        ExpTick,
        LevelUp,
        BuffApplied,
        DebuffApplied,
        ItemEquip,
        ItemMove,
        GoldGain,

        // Turn / phase
        PhasePlayer,
        PhaseEnemy,
        PhaseAllied,
        Fanfare,

        // Dialogue
        DialogueBlip,

        // Scene / music
        MusicTitle,
        MusicVictory,
        MusicGameOver,
        TransitionWhoosh,
        SplashSting,

        // Grid cursor
        CursorMove,
    }
}
