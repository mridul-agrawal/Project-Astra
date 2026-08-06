using UnityEngine;

namespace ProjectAstra.Core.Turn
{
    // Whether a phase-start announcement is currently on screen.
    //
    // The banner owns the animation; this is just the fact of it. It lives here rather than on
    // the banner so gameplay — the cursor, the turn manager — can gate on "a phase is being
    // announced" without taking a dependency on a particular UI widget, and so a scene with no
    // banner in it simply never sets the flag.
    public static class PhaseIntro
    {
        public static bool IsPlaying { get; private set; }

        public static void Begin() => IsPlaying = true;
        public static void End() => IsPlaying = false;

        // Entering play mode with domain reload disabled keeps statics from the last session; a
        // flag stuck on would leave the cursor dead on arrival.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => IsPlaying = false;
    }
}
