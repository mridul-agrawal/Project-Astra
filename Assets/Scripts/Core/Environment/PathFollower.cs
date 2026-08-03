using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Environment
{
    // Glides an object along a smooth path defined by waypoints (offsets from its
    // start), following the Catmull-Rom curve through them. This is the "bird flying
    // A to B on an arc" motion. A small random jitter re-shapes the arc each pass so
    // it never repeats exactly.
    //
    // Modes:
    //   Loop      — A→B, snap back, A→B… (continuous, one direction)
    //   PingPong  — A→B→A→B… (continuous, back and forth)
    //   Once      — A→B, then stop for good
    //   PerchHop  — sit at A a random dwell, fly to B, sit a random dwell, fly back… —
    //               a bird hopping between branches at unhurried, random intervals.
    public sealed class PathFollower : MonoBehaviour
    {
        public enum Mode { Loop, PingPong, Once, PerchHop }

        [SerializeField] private Vector2[] waypoints = { Vector2.zero, new(6f, 2f), new(12f, 0f) };
        [SerializeField] private float speed = 2f;
        [SerializeField] private Mode mode = Mode.Loop;
        [SerializeField] private float jitterAmplitude = 0.5f;
        [SerializeField] private bool flipToFaceTravel = true;

        [Tooltip("PerchHop only: seconds (min, max) the object waits at each end before the next flight.")]
        [SerializeField] private Vector2 dwellRange = new(3f, 8f);

        [Tooltip("PerchHop only: freeze on the first sprite while perched, so it only animates (flaps) mid-flight.")]
        [SerializeField] private bool pauseAnimationWhilePerched = true;

        private Vector3 origin;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Sprite restingSprite;
        private readonly List<Vector2> points = new();
        private float t;
        private int direction = 1;
        private float perchTimer;

        private void Start()
        {
            origin = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            restingSprite = spriteRenderer != null ? spriteRenderer.sprite : null;   // the seeded first frame
            RebuildPath();
            if (mode == Mode.PerchHop) { perchTimer = RandomDwell(); FreezeAnimation(); }   // start settled + still
        }

        private void Update()
        {
            if (points.Count < 2) return;

            if (perchTimer > 0f)
            {
                perchTimer -= Time.deltaTime;
                if (perchTimer <= 0f) { RebuildPath(); ResumeAnimation(); }   // re-shape the arc, start flapping
                return;
            }

            Advance();
            Vector2 offset = CatmullRom.Evaluate(points, t);
            Vector3 previous = transform.position;
            transform.position = origin + new Vector3(offset.x, offset.y, 0f);

            if (flipToFaceTravel && spriteRenderer != null)
            {
                float dx = transform.position.x - previous.x;
                if (Mathf.Abs(dx) > 0.0001f) spriteRenderer.flipX = dx < 0f;
            }
        }

        // Advances t along the path and handles the end of a leg per Mode.
        private void Advance()
        {
            float pathLength = Mathf.Max(EstimateLength(), 0.001f);
            t += direction * (speed / pathLength) * Time.deltaTime;

            if (mode == Mode.PerchHop)
            {
                if (t >= 1f) { t = 1f; StartPerch(); }
                else if (t <= 0f) { t = 0f; StartPerch(); }
                return;
            }

            if (t >= 1f)
            {
                if (mode == Mode.Loop) { t = 0f; RebuildPath(); }
                else if (mode == Mode.PingPong) { t = 1f; direction = -1; }
                else { t = 1f; enabled = false; }
            }
            else if (t <= 0f && mode == Mode.PingPong)
            {
                t = 0f; direction = 1; RebuildPath();
            }
        }

        // Landed on a branch: go still, wait a random spell, then head back.
        private void StartPerch()
        {
            perchTimer = RandomDwell();
            direction = -direction;
            FreezeAnimation();
        }

        private float RandomDwell() => Random.Range(dwellRange.x, dwellRange.y);

        // Perched: stop the flap and show the first sprite. The Animator is off, so
        // it stops driving the SpriteRenderer and our resting frame holds.
        private void FreezeAnimation()
        {
            if (!pauseAnimationWhilePerched) return;
            if (animator != null) animator.enabled = false;
            if (restingSprite != null && spriteRenderer != null) spriteRenderer.sprite = restingSprite;
        }

        // Taking off: hand the SpriteRenderer back to the Animator so it flaps.
        private void ResumeAnimation()
        {
            if (!pauseAnimationWhilePerched) return;
            if (animator != null) animator.enabled = true;
        }

        // Copies the authored waypoints and nudges the interior ones sideways so
        // each pass takes a slightly different arc.
        private void RebuildPath()
        {
            points.Clear();
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector2 p = waypoints[i];
                if (jitterAmplitude > 0f && i > 0 && i < waypoints.Length - 1)
                    p += new Vector2(Random.Range(-jitterAmplitude, jitterAmplitude),
                                     Random.Range(-jitterAmplitude, jitterAmplitude));
                points.Add(p);
            }
        }

        private float EstimateLength()
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++) total += Vector2.Distance(points[i - 1], points[i]);
            return total;
        }
    }
}
