using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Hub
{
    // Walks the protagonist and works out what a press in front of her would act on.
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
            if (router != null) router.Gate.HandoverEnded += Interaction.SuppressCurrentPress;
        }

        private void OnEnable() => EventService.Instance?.SubscribeGameStateChanged(OnGameStateChanged);

        private void OnDisable() => EventService.Instance?.UnsubscribeGameStateChanged(OnGameStateChanged);

        private void OnDestroy()
        {
            if (router != null) router.Gate.HandoverEnded -= Interaction.SuppressCurrentPress;
        }

        private void Update()
        {
            if (router == null || HubLocationService.Instance == null) return;

            Walk(router.MoveIntent);

            // After the walk, so the reach check uses where she ended up this frame rather than
            // where she started it.
            Interaction.Enabled = router.Gate.AcceptsWorldInteraction;
            Interaction.Tick(new InteractorPose(actor.Position, actor.Facing), IsConfirmHeld);
        }

        private void Walk(Facing? intent)
        {
            if (intent.HasValue) actor.SetFacing(intent.Value);

            Vector2 next = HubMover.Move(
                HubLocationService.Instance.Collision,
                actor.Position, actor.FootprintOffset,
                intent, Time.deltaTime, out bool moved, speedTilesPerSecond);

            IsWalking = moved;
            if (moved) actor.SetPosition(next);
        }

        // Read raw rather than taking the router's edge, because the press latch that turns it into
        // one interaction lives on the interaction controller and is what suppression re-arms.
        private static bool IsConfirmHeld =>
            InputManager.Instance != null && InputManager.Instance.IsActionHeld(GameInputAction.Confirm);

        // A conversation or a scripted sequence taking over must not leave a held button armed for
        // the moment control comes back.
        private void OnGameStateChanged(StateChangeArgs args) => Interaction.SuppressCurrentPress();
    }
}
