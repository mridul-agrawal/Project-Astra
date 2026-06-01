using System.Collections.Generic;

namespace ProjectAstra.Core.Combat.Playback
{
    // The result of BraveFlowComposer — a flat list of playback steps the
    // overlay controller walks in order. Each step knows everything the
    // controller needs (which side is striking, what HP transition to play,
    // crit context for visuals/timing). Skip mode bypasses this and works
    // straight off CombatResult.Hits — brave fusion is invisible at that
    // cadence so there is nothing to compose.
    public class CombatPlan
    {
        public List<PlaybackStep> Steps;
    }

    public abstract class PlaybackStep
    {
        // Which side is the striking unit. The defender is always "the other".
        public bool AttackerIsStriker;
    }

    // A single hit with its full per-phase timeline (windup → strike → resolve
    // → follow-through → inter-hit gap). The shipped Phase B path.
    public class SingleHitStep : PlaybackStep
    {
        public HitResult Hit;
        public CritContext CritContext;  // Only consulted when Hit.Crit is true.
        public bool FatalHit;            // Strike that drops receiver to 0 HP.
    }

    // A brave-weapon fusion: two consecutive hits from the same side. The
    // first hit's wind-up plays, then strike1, then a short wind-up (no
    // full pull-back, no inter-hit gap), then strike2, resolve, follow-through.
    // Defender plays one continuous recoil tween spanning both strikes; HP
    // bar drains across the combined damage in a single tween.
    public class BraveHitStep : PlaybackStep
    {
        public HitResult Hit1;
        public HitResult Hit2;
        public CritContext Hit1CritContext;
        public CritContext Hit2CritContext;
        public bool Hit1Fatal;
        public bool Hit2Fatal;
    }
}
