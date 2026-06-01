namespace ProjectAstra.Core.Combat.Playback
{
    // Player-facing combat-animation speed setting (UI-10).
    public enum CombatAnimationSpeed
    {
        Normal,   // Full overlay scene at base speed; voice + SFX.
        Fast,     // Same overlay, all phase timings halved; voice suppressed.
        Skip,     // No overlay; combat resolves on the map with damage floats + HP drain.
    }
}
