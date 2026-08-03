using UnityEngine;
using UnityEngine.Events;

namespace ProjectAstra.Core.Environment
{
    // Counts down a random interval, then fires — over and over. This is the
    // "non-continuous" behaviour: a leaf that drops every few seconds instead of
    // an animation that loops forever. Kept pure so it can be unit-tested.
    public struct IntervalScheduler
    {
        private float remaining;

        // Advances by dt and returns true on the frame the interval elapses,
        // rescheduling the next interval from [min, max].
        public bool Tick(float dt, float min, float max)
        {
            remaining -= dt;
            if (remaining > 0f) return false;
            remaining = Random.Range(min, max);
            return true;
        }

        public void Reset(float min, float max) => remaining = Random.Range(min, max);
    }

    // Fires an action at random intervals. Point it at an Animator trigger (a
    // falling-leaf clip), a ParticleSystem burst, or wire the UnityEvent to any
    // coder-authored effect. The interval randomness keeps repeated props from
    // acting in unison.
    public sealed class RandomIntervalTrigger : MonoBehaviour
    {
        [SerializeField] private Vector2 intervalRange = new(3f, 7f);
        [SerializeField] private bool useUnscaledTime;

        [Header("What to fire (any/all)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string animatorTrigger = "";
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private UnityEvent onFire;

        private IntervalScheduler scheduler;

        private void OnEnable() => scheduler.Reset(intervalRange.x, intervalRange.y);

        private void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (scheduler.Tick(dt, intervalRange.x, intervalRange.y))
                Fire();
        }

        private void Fire()
        {
            if (animator != null && !string.IsNullOrEmpty(animatorTrigger)) animator.SetTrigger(animatorTrigger);
            if (particles != null) particles.Play();
            onFire?.Invoke();
        }
    }
}
