using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Hub
{
    // Walks the protagonist and works out what a press in front of her would act on.
    [RequireComponent(typeof(HubActor))]
    public sealed class HubPlayerController : MonoBehaviour
    {
        [Tooltip("Tiles per second. 3.5 is 112 px/s at 32px tiles — a designer-tunable feel value.")]
        [SerializeField] private float speedTilesPerSecond = HubMover.DefaultSpeedTilesPerSecond;

        private readonly CardinalInputResolver directions = new();
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
            if (Gate != null) Gate.HandoverEnded += Interaction.SuppressCurrentPress;
        }

        private void OnEnable() => EventService.Instance?.SubscribeGameStateChanged(OnGameStateChanged);

        private void OnDisable() => EventService.Instance?.UnsubscribeGameStateChanged(OnGameStateChanged);

        private void OnDestroy()
        {
            if (Gate != null) Gate.HandoverEnded -= Interaction.SuppressCurrentPress;
        }

        private void Update()
        {
            if (HubLocationService.Instance == null) return;

            // Resolved every frame even while control is locked, so a direction held across a
            // conversation keeps walking the moment control comes back.
            Facing? walking = ResolveHeldDirection();
            bool inControl = Gate == null || Gate.AcceptsMovement;

            Walk(inControl ? walking : null);

            // After the walk, so the reach check uses where she ended up this frame rather than
            // where she started it.
            Interaction.Enabled = inControl;
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

        private static HubControlGate Gate => HubControlGate.Instance;

        private Facing? ResolveHeldDirection() => directions.Resolve(
            IsHeld(GameInputAction.CursorUp),
            IsHeld(GameInputAction.CursorDown),
            IsHeld(GameInputAction.CursorRight),
            IsHeld(GameInputAction.CursorLeft));

        // Read raw rather than edge-detected here, because the press latch that turns it into one
        // interaction lives on the interaction controller and is what suppression re-arms.
        private static bool IsConfirmHeld => IsHeld(GameInputAction.Confirm);

        private static bool IsHeld(GameInputAction action) =>
            InputManager.Instance != null && InputManager.Instance.IsActionHeld(action);

        // A conversation or a scripted sequence taking over must not leave a held button armed for
        // the moment control comes back.
        private void OnGameStateChanged(StateChangeArgs args) => Interaction.SuppressCurrentPress();

        // A lost window or an unplugged controller leaves directions stuck down, so drop them and
        // wait for real input to come back.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) directions.Clear();
        }
    }
}
