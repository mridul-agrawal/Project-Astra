using System;
using System.Collections;
using ProjectAstra.Core.Combat.Playback;

namespace ProjectAstra.Core.Combat
{
    // Per-map combat scripting seams. A battle director installs these at battle start
    // and must Clear() on teardown; null means honest behavior (real dice, real growth
    // rolls, no playback hook). Maps without a director never touch this class.
    public static class CombatScriptOverride
    {
        public static IRng Rng;
        public static Func<int, bool> GrowthRoll;
        public static Func<CombatPlaybackContext, int, IEnumerator> PreStepHook;

        public static void Clear()
        {
            Rng = null;
            GrowthRoll = null;
            PreStepHook = null;
        }
    }
}
