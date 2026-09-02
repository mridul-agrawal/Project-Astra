using UnityEngine;
using ProjectAstra.Core.Animation;

using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Hub
{
    // Walks the protagonist. Reads the router's resolved direction, asks HubMover where that
    // lands against the collision map, and puts her there.
    //
    // Runs after HubInputRouter, which resolves the direction earlier in the frame.
    [RequireComponent(typeof(HubActor))]
    public sealed class HubPlayerController : MonoBehaviour
    {
        [SerializeField] private HubInputRouter router;

        [Tooltip("Tiles per second. 3.5 is 112 px/s at 32px tiles — a designer-tunable feel value.")]
        [SerializeField] private float speedTilesPerSecond = HubMover.DefaultSpeedTilesPerSecond;

        private HubActor actor;

        // True while she is actually changing position, so the interaction layer can stop her
        // before a conversation opens.
        public bool IsWalking { get; private set; }

        // What is in reach and what a press would act on. Owned here because this is the thing
        // that knows where she is; interactables find it through the trigger she carries.
        public PlayerInteractionController Interaction { get; } = new();

        private void Awake()
        {
            actor = GetComponent<HubActor>();
            if (router == null) router = FindFirstObjectByType<HubInputRouter>();
        }

        private void Update()
        {
            if (router == null || HubLocationService.Instance == null) return;

            Facing? intent = router.MoveIntent;
            if (intent.HasValue) actor.SetFacing(intent.Value);

            Vector2 next = HubMover.Move(
                HubLocationService.Instance.Collision,
                actor.Position, actor.FootprintOffset,
                intent, Time.deltaTime, out bool moved, speedTilesPerSecond);

            IsWalking = moved;
            if (moved) actor.SetPosition(next);
        }
    }
}
