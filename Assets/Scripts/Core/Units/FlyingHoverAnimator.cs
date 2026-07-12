using UnityEngine;

namespace ProjectAstra.Core.Units
{
    // Sinusoidal vertical bob for flying-unit sprites — a visual cue that the
    // unit ignores terrain movement costs. Attach to the sprite child (not the
    // root TestUnit) so only visuals bob; the root transform stays tile-snapped
    // for grid-position logic.
    //
    // Phase is randomised per instance so a squad of fliers doesn't oscillate
    // in lockstep.
    public class FlyingHoverAnimator : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.25f;
        [SerializeField] private float periodSeconds = 1.2f;

        private float baseLocalY;
        private float phaseOffset;

        private void Awake()
        {
            baseLocalY = transform.localPosition.y;
            phaseOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        private void Update()
        {
            if (periodSeconds <= 0f) return;

            float omega = 2f * Mathf.PI / periodSeconds;
            float y = baseLocalY + Mathf.Sin(Time.time * omega + phaseOffset) * amplitude;
            var p = transform.localPosition;
            p.y = y;
            transform.localPosition = p;
        }
    }
}
