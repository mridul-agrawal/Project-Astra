using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.Gurukul
{
    // The one place the hub reads input, and the reason a single button press can never mean two
    // things at once.
    //
    // InputManager.OnConfirm is a plain multicast event with no ordering and no way to mark a press
    // consumed — DialogueService and the selection menu both subscribe to it already, so two
    // handlers can fire on one press. Rather than add a third subscriber, the hub polls, decides
    // what the press means from its own sub-state, and publishes exactly one of the fields below.
    // There is a single confirm latch behind all three, so the same press physically cannot be read
    // as both "talk to him" and "advance the line".
    //
    // Polling also sidesteps DelayedAutoShift entirely. Its 0.4s-then-0.1s cadence is grid-cursor
    // feel; walking needs the raw held state, and the battle map still needs DAS untouched.
    [DefaultExecutionOrder(-50)]
    public sealed class GurukulInputRouter : MonoBehaviour
    {
        private readonly CardinalInputResolver directions = new();
        private readonly InteractLatch confirm = new();
        private readonly InteractLatch cancel = new();
        private readonly InteractLatch skip = new();
        private readonly InteractLatch menuUp = new();
        private readonly InteractLatch menuDown = new();

        public GurukulStateMachine States { get; private set; }

        // Walking. Null means stand still. Free exploration only.
        public Facing? MoveIntent { get; private set; }

        // Each true for exactly one frame, and only in the state that owns it.
        public bool InteractPressed { get; private set; }
        public bool AdvancePressed { get; private set; }
        public bool SkipPressed { get; private set; }
        public bool ConfirmPressed { get; private set; }
        public bool CancelPressed { get; private set; }

        // -1 for up, +1 for down, 0 for nothing. One step per press — menus don't auto-repeat.
        public int MenuStep { get; private set; }

        private void Awake()
        {
            States = new GurukulStateMachine();
            States.StateChanged += OnSubStateChanged;
        }

        private void OnDestroy()
        {
            if (States != null) States.StateChanged -= OnSubStateChanged;
        }

        private void Update()
        {
            ClearLastFrame();

            // Resolved every frame even when movement is locked, so the held-direction history stays
            // current and a direction held across a conversation keeps walking the moment it ends.
            Facing? walking = ResolveHeldDirection();
            bool confirmPressed = confirm.Consume(IsHeld(GameInputAction.Confirm));

            switch (States.CurrentState)
            {
                case GurukulSubState.FreeExploration:
                    MoveIntent = walking;
                    InteractPressed = confirmPressed;
                    break;

                case GurukulSubState.Conversation:
                    AdvancePressed = confirmPressed;
                    SkipPressed = skip.Consume(IsHeld(GameInputAction.SkipDialogue));
                    break;

                case GurukulSubState.ChoiceOrQuiz:
                    ConfirmPressed = confirmPressed;
                    CancelPressed = cancel.Consume(IsHeld(GameInputAction.Cancel));
                    MenuStep = ResolveMenuStep();
                    break;
            }
        }

        private void ClearLastFrame()
        {
            MoveIntent = null;
            InteractPressed = false;
            AdvancePressed = false;
            SkipPressed = false;
            ConfirmPressed = false;
            CancelPressed = false;
            MenuStep = 0;
        }

        private Facing? ResolveHeldDirection() => directions.Resolve(
            IsHeld(GameInputAction.CursorUp),
            IsHeld(GameInputAction.CursorDown),
            IsHeld(GameInputAction.CursorRight),
            IsHeld(GameInputAction.CursorLeft));

        private int ResolveMenuStep()
        {
            bool up = menuUp.Consume(IsHeld(GameInputAction.CursorUp));
            bool down = menuDown.Consume(IsHeld(GameInputAction.CursorDown));
            if (up) return -1;
            return down ? 1 : 0;
        }

        private static bool IsHeld(GameInputAction action) =>
            InputManager.Instance != null && InputManager.Instance.IsActionHeld(action);

        // Every change of state re-arms the buttons, so a press held through the end of a
        // conversation can't immediately activate whatever she happens to be standing in front of.
        private void OnSubStateChanged(GurukulSubState previous, GurukulSubState next)
        {
            confirm.Suppress();
            cancel.Suppress();
            skip.Suppress();
            menuUp.Suppress();
            menuDown.Suppress();
        }

        // A lost window or an unplugged controller leaves directions stuck down, so drop them and
        // wait for real input to come back.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) directions.Clear();
        }

        private void OnDisable()
        {
            directions.Clear();
            ClearLastFrame();
        }
    }
}
