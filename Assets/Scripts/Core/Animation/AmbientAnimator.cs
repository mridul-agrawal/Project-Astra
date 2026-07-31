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

        private void Start()
        {
            var animator = GetComponent<Animator>();
            if (useUnscaledTime) animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            int stateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            animator.Play(stateHash, 0, Random.value * maxStartOffset);
        }
    }
}
