using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Sinusoidal vertical bob for flying-unit map sprites, giving the visual cue
    /// that the unit ignores terrain movement costs. Attach to the sprite child
    /// (not the root TestUnit) so only visuals bob — grid-position logic still
    /// reads the root transform, which stays tile-snapped.
    ///
    /// Phase is randomised per instance so a squad of fliers doesn't oscillate
    /// in lockstep.
    /// </summary>
    public class FlyingHoverAnimator : MonoBehaviour
    {
        [SerializeField] private float _amplitude = 0.25f;
        [SerializeField] private float _periodSeconds = 1.2f;

        private float _baseLocalY;
        private float _phaseOffset;

        private void Awake()
        {
            _baseLocalY = transform.localPosition.y;
            _phaseOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        private void Update()
        {
            if (_periodSeconds <= 0f) return;

            float omega = 2f * Mathf.PI / _periodSeconds;
            float y = _baseLocalY + Mathf.Sin(Time.time * omega + _phaseOffset) * _amplitude;
            var p = transform.localPosition;
            p.y = y;
            transform.localPosition = p;
        }
    }
}
