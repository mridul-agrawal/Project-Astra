using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // Ping-pongs a decoration between its start and start+offset — the "bird
    // flying from one point to another" motion. Pairs with an AmbientAnimator
    // for the wing flap. Position-only, so it never fights a frame animation.
    public sealed class WaypointMover : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(6f, 0f, 0f);
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool flipToFaceTravel = true;

        private Vector3 start;
        private SpriteRenderer spriteRenderer;

        private void Start()
        {
            start = transform.localPosition;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float t = Mathf.PingPong(Time.time * speed, 1f);
            transform.localPosition = Vector3.Lerp(start, start + offset, t);

            if (flipToFaceTravel && spriteRenderer != null && offset.x != 0f)
            {
                bool goingForward = Mathf.PingPong(Time.time * speed + 0.5f, 1f) > t;
                spriteRenderer.flipX = goingForward ? offset.x < 0f : offset.x > 0f;
            }
        }
    }
}
