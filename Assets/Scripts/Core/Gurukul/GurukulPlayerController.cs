using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // Walks the protagonist. Reads the router's resolved direction, asks GurukulMover where that
    // lands against the collision map, and puts her there.
    //
    // Runs after GurukulInputRouter, which resolves the direction earlier in the frame.
    [RequireComponent(typeof(GurukulActor))]
    public sealed class GurukulPlayerController : MonoBehaviour
    {
        [SerializeField] private GurukulInputRouter router;

        [Tooltip("Tiles per second. 3.5 is 112 px/s at 32px tiles — a designer-tunable feel value.")]
        [SerializeField] private float speedTilesPerSecond = GurukulMover.DefaultSpeedTilesPerSecond;

        private GurukulActor actor;

        // True while she is actually changing position, so the interaction layer can stop her
        // before a conversation opens.
        public bool IsWalking { get; private set; }

        private void Awake()
        {
            actor = GetComponent<GurukulActor>();
            if (router == null) router = FindFirstObjectByType<GurukulInputRouter>();
        }

        private void Update()
        {
            if (router == null || GurukulLocationService.Instance == null) return;

            Facing? intent = router.MoveIntent;
            if (intent.HasValue) actor.SetFacing(intent.Value);

            Vector2 next = GurukulMover.Move(
                GurukulLocationService.Instance.Collision,
                actor.Position, actor.FootprintOffset,
                intent, Time.deltaTime, out bool moved, speedTilesPerSecond);

            IsWalking = moved;
            if (moved) actor.SetPosition(next);
        }
    }
}
