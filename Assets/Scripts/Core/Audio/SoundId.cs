namespace ProjectAstra.Core.Audio
{
    // Append new ids at the end of a group. Reordering shifts saved references.
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
