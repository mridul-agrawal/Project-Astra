using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Drives a sinusoidal alpha pulse on a SpriteRenderer for cursor idle animation.
    /// Uses Time.time for frame-rate-independent, deterministic oscillation.
    /// </summary>
    public class CursorAnimator
    {
        private readonly SpriteRenderer _renderer;
        private readonly float _speed;
        private readonly float _alphaMin;
        private readonly float _alphaMax;

        public CursorAnimator(SpriteRenderer renderer, float speed, float alphaMin, float alphaMax)
        {
            _renderer = renderer;
            _speed = speed;
            _alphaMin = alphaMin;
            _alphaMax = alphaMax;
        }

        public void UpdatePulse()
        {
            if (_renderer == null) return;

            float t = (Mathf.Sin(Time.time * _speed * 2f * Mathf.PI) + 1f) / 2f;
            float alpha = Mathf.Lerp(_alphaMin, _alphaMax, t);

            Color c = _renderer.color;
            c.a = alpha;
            _renderer.color = c;
        }
    }
}
