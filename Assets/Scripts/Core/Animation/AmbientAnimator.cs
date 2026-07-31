using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // Small helper for looping environment and base-layer animations. Nudges the
    // Animator to a random start phase so identical clips (a row of grass, a tiled
    // river) don't all pulse in lockstep, and optionally keeps animating on
    // unscaled time so ambient life continues behind a pause menu.
    [RequireComponent(typeof(Animator))]
    public sealed class AmbientAnimator : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float maxStartOffset = 1f;
        [SerializeField] private bool useUnscaledTime;

        private float explicitPhase = -1f;   // set by Configure; < 0 keeps the random start

        // Called by the runtime spawner before Start so a designer-authored phase is
        // deterministic — two rivers can be offset by exactly 0.5 instead of randomly.
        public void Configure(float phaseOffset, bool unscaledTime)
        {
            explicitPhase = Mathf.Clamp01(phaseOffset);
            useUnscaledTime = unscaledTime;
        }

        private void Start()
        {
            var animator = GetComponent<Animator>();
            if (useUnscaledTime) animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            int stateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            float phase = explicitPhase >= 0f ? explicitPhase : Random.value * maxStartOffset;
            animator.Play(stateHash, 0, phase);
        }
    }
}
