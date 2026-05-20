using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Drives a sinusoidal alpha + scale pulse on the cursor SpriteRenderer.
    // Uses Time.time so the oscillation is frame-rate independent and
    // deterministic across runs.
    public class CursorAnimator
    {
        private readonly SpriteRenderer _renderer;
        private readonly Transform _transform;

        public CursorAnimator(SpriteRenderer renderer)
        {
            _renderer = renderer;
            _transform = renderer != null ? renderer.transform : null;
        }

        public void UpdatePulse(float speed, float alphaMin, float alphaMax,
            float scaleMin, float scaleMax)
        {
            if (_renderer == null) return;

            // 0..1 oscillator mapped onto the alpha and scale ranges.
            float t = (Mathf.Sin(Time.time * speed * 2f * Mathf.PI) + 1f) / 2f;

            float alpha = Mathf.Lerp(alphaMin, alphaMax, t);
            Color c = _renderer.color;
            c.a = alpha;
            _renderer.color = c;

            float scale = Mathf.Lerp(scaleMin, scaleMax, t);
            _transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
