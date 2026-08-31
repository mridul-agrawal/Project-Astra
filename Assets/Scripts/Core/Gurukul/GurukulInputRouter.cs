using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Gurukul
{
    // The one place the hub reads input, and the reason a single button press can never mean two
    // things at once.
    //
    // Only walking and interacting live here now. Advancing a line and picking an option belong to
    // ConversationPlayer, which owns the confirm button for as long as GameState.Dialogue is up —
    // and this router publishes nothing at all while that is the case, so the two never overlap.
    //
    // Polling rather than subscribing also sidesteps DelayedAutoShift entirely. Its 0.4s-then-0.1s
    // cadence is grid-cursor feel; walking needs the raw held state, and the battle map still needs
    // DAS untouched.
    [DefaultExecutionOrder(-50)]
    public sealed class GurukulInputRouter : MonoBehaviour
    {
        private readonly CardinalInputResolver directions = new();
        private readonly InteractLatch confirm = new();

        public GurukulControlGate Gate { get; private set; }

        // Walking. Null means stand still. Free exploration only.
        public Facing? MoveIntent { get; private set; }

        // True for exactly one frame, and only when the hub is the thing in control.
        public bool InteractPressed { get; private set; }

        private void Awake()
        {
            Gate = new GurukulControlGate();
            Gate.HandoverEnded += OnHandoverEnded;
        }

        private void OnEnable() => EventService.Instance?.SubscribeGameStateChanged(OnGameStateChanged);

        private void OnDestroy()
        {
            if (Gate != null) Gate.HandoverEnded -= OnHandoverEnded;
        }

        private void Update()
        {
            ClearLastFrame();

            // Resolved every frame even when movement is locked, so the held-direction history
            // stays current and a direction held across a conversation keeps walking the moment
            // control comes back.
            Facing? walking = ResolveHeldDirection();
            bool confirmPressed = confirm.Consume(IsHeld(GameInputAction.Confirm));

            if (!Gate.AcceptsMovement) return;

            MoveIntent = walking;
            InteractPressed = confirmPressed;
        }

        private void ClearLastFrame()
        {
            MoveIntent = null;
            InteractPressed = false;
        }

        private Facing? ResolveHeldDirection() => directions.Resolve(
            IsHeld(GameInputAction.CursorUp),
            IsHeld(GameInputAction.CursorDown),
            IsHeld(GameInputAction.CursorRight),
            IsHeld(GameInputAction.CursorLeft));

        private static bool IsHeld(GameInputAction action) =>
            InputManager.Instance != null && InputManager.Instance.IsActionHeld(action);

        // Both re-arm the button, so a press held through the end of a conversation or a doorway
        // can't immediately activate whatever she happens to be standing in front of.
        private void OnHandoverEnded() => confirm.Suppress();

        private void OnGameStateChanged(StateChangeArgs args) => confirm.Suppress();

        // A lost window or an unplugged controller leaves directions stuck down, so drop them and
        // wait for real input to come back.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) directions.Clear();
        }

        private void OnDisable()
        {
            EventService.Instance?.UnsubscribeGameStateChanged(OnGameStateChanged);
            directions.Clear();
            ClearLastFrame();
        }
    }
}
