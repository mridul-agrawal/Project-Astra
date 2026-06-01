using UnityEngine;

namespace ProjectAstra.Core.Combat.Playback
{
    // Speed-aware wait helpers. Reads CombatAnimationSettingsRef.Current at
    // every call so per-combat overrides take effect immediately.
    //
    // Phase B introduced WaitPhase / PhaseDuration for the overlay playback's
    // five-phase per-hit timeline. Skip mode is its own controller and uses
    // WaitSeconds directly with its own gap constants.
    public static class CombatTiming
    {
        public enum Phase
        {
            Windup,         // Attacker lunges forward; voice bark at start (Normal only — Phase D)
            Strike,         // Contact pose; HP write + impact SFX + hit flash + damage label
            Resolve,        // Hold impact frame; HP bar drains across this + follow-through
            FollowThrough,  // Attacker returns to idle; defender recoil settles
            InterHitGap,    // Breath before next step (0 inside a brave-pair — Phase E)
        }

        // Base durations (Normal-mode). Fast halves; Skip drives the same hits
        // through SkipModePlaybackController with its own gap timing.
        private const float WindupSeconds        = 0.45f;
        private const float StrikeSeconds        = 0.10f;
        private const float ResolveSeconds       = 0.20f;
        private const float FollowThroughSeconds = 0.35f;
        private const float InterHitGapSeconds   = 0.20f;

        public static CombatAnimationSpeed EffectiveSpeed =>
            CombatAnimationSettingsRef.Current != null
                ? CombatAnimationSettingsRef.Current.EffectiveSpeed
                : CombatAnimationSpeed.Normal;

        public static WaitForSeconds WaitSeconds(float seconds) =>
            new WaitForSeconds(Mathf.Max(0f, seconds));

        public static WaitForSeconds WaitPhase(Phase phase) =>
            new WaitForSeconds(Mathf.Max(0f, PhaseDuration(phase)));

        // Float exposed so tweens that need the raw duration (e.g. HP-bar
        // drain spanning resolve + follow-through) don't have to duplicate
        // the scaling logic.
        public static float PhaseDuration(Phase phase) =>
            BaseDurationFor(phase) * SpeedScaleFor(EffectiveSpeed);

        // Voice barks: Normal only (Fast would overlap them at 2× speed,
        // Skip has no room in 0.15s gaps). Wired in Phase D.
        public static bool ShouldPlayVoice() => EffectiveSpeed == CombatAnimationSpeed.Normal;

        private static float BaseDurationFor(Phase phase)
        {
            switch (phase)
            {
                case Phase.Windup:        return WindupSeconds;
                case Phase.Strike:        return StrikeSeconds;
                case Phase.Resolve:       return ResolveSeconds;
                case Phase.FollowThrough: return FollowThroughSeconds;
                case Phase.InterHitGap:   return InterHitGapSeconds;
                default:                  return 0f;
            }
        }

        private static float SpeedScaleFor(CombatAnimationSpeed speed)
        {
            switch (speed)
            {
                case CombatAnimationSpeed.Normal: return 1.0f;
                case CombatAnimationSpeed.Fast:   return 0.5f;
                case CombatAnimationSpeed.Skip:   return 0f;
                default:                          return 1.0f;
            }
        }
    }
}
