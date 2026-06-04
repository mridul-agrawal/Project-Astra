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
    }
}
