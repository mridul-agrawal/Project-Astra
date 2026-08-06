using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.Input
{
    // Which device most recently produced input. Used by the HUD to swap glyph sets
    // (keyboard prompts vs. gamepad prompts).
    public enum InputDeviceType { Keyboard, Gamepad, Mouse }

    // Singleton bridge between Unity's Input System and game-level events. Three jobs:
    //   1. Translate raw action callbacks into typed events (OnCursorMove, OnConfirm, ...).
    //   2. Filter actions by the current GameState (no Confirm during CombatAnimation, etc.).
    //   3. Drive a DelayedAutoShift sub-controller for held cursor directions, and
    //      resolve same-frame Confirm/Cancel conflicts (Cancel wins).
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private InputContextTable inputContextTable;

        [Header("DAS Settings")]
        [SerializeField] private float dasInitialDelay = 0.4f;
        [SerializeField] private float dasRepeatRate = 0.1f;
        [SerializeField] private float dasFastRepeatRate = 0.05f;

        public event Action<Vector2Int> OnCursorMove;
        public event Action OnConfirm;
        public event Action OnCancel;
        public event Action OnOpenMapMenu;
        public event Action OnOpenUnitInfo;
        public event Action OnToggleMapOverlay;
        public event Action OnPause;
        public event Action OnSkipAnimation;
        public event Action OnSkipDialogue;
        public event Action<bool> OnHoldAdvanceDialogue;
        public event Action OnNextUnit;
        public event Action OnPrevUnit;
        public event Action<bool> OnPeekObjective;
        public event Action<InputDeviceType> OnDeviceChanged;
        public event Action OnGamepadDisconnected;

        public InputDeviceType ActiveDevice { get; private set; } = InputDeviceType.Keyboard;
        public bool IsFastCursorHeld { get; private set; }

        private InputActionMap gameplayMap;
        private GameState currentState;
        private DelayedAutoShift das;

        // Removers for every action callback we bind, so we can detach them on destroy.
        // Without this, the InputActionAsset (a persistent project asset) keeps our
        // callbacks alive across editor play sessions when Domain Reload is disabled,
        // firing stale handlers into already-destroyed objects.
        private readonly List<Action> actionUnbinders = new();

        private bool phaseIntroWasPlaying;
        private bool confirmPendingThisFrame;
        private bool cancelPendingThisFrame;
        private bool stateSubscribed;

        private void Awake()
        {
            CreateSingleton();
            CreateDelayedAutoShift();
            InitializeInputActionMap();
        }

        private void CreateDelayedAutoShift()
        {
            das = new DelayedAutoShift(dasInitialDelay, dasRepeatRate, dasFastRepeatRate);
            das.CursorMoveTriggered += direction => OnCursorMove?.Invoke(direction);
        }

        private void OnEnable()
        {
            InputSystem.onActionChange += OnInputActionChange;
        }

        // Subscribe in Start (not OnEnable) so EventService.Instance is guaranteed set —
        // this component boots alongside EventService, so OnEnable ordering isn't safe.
        private void Start()
        {
            EventService.Instance.SubscribeGameStateChanged(OnStateChanged);
            stateSubscribed = true;

            currentState = GameStateManager.Instance.CurrentState;
            ApplyContextFilter(currentState);
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnInputActionChange;
        }

        // The action callbacks live on the InputActionAsset, which outlives this
        // component (and survives play-session restarts when Domain Reload is off).
        // Detach them here so a destroyed InputManager can't keep handling input.
        private void OnDestroy()
        {
            if (stateSubscribed && EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnStateChanged);

            UnbindActions();
            gameplayMap?.Disable();
        }

        private void Update()
        {
            ApplyCursorProfileTimings();
            ReArmRepeatAfterPhaseIntro();
            das.Tick(Time.deltaTime, IsFastCursorHeld);
            ResolveSameFramePriority();
        }

        // A direction held through a phase announcement has long since passed its initial
        // delay, so control returning would dash the cursor across the map. Re-arm on the way
        // out so the hold has to earn its repeat again.
        private void ReArmRepeatAfterPhaseIntro()
        {
            bool playing = PhaseIntro.IsPlaying;
            if (phaseIntroWasPlaying && !playing) das.Reset();
            phaseIntroWasPlaying = playing;
        }

        // Held repeat is a cursor feel parameter, so it lives on the cursor variant profile
        // alongside the slide and the breath. The serialized fields above stay as the
        // fallback for scenes with no cursor in them.
        private void ApplyCursorProfileTimings()
        {
            var profile = CursorVisualDirector.Current != null
                ? CursorVisualDirector.Current.ActiveProfile
                : null;
            if (profile == null) return;

            das.SetTimings(profile.HeldRepeatDelay, profile.HeldRepeatStep, dasFastRepeatRate);
        }

        private void LateUpdate()
        {
            confirmPendingThisFrame = false;
            cancelPendingThisFrame = false;
        }

        private void CreateSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeInputActionMap()
        {
            gameplayMap = inputActions.FindActionMap("Gameplay");
            if (gameplayMap == null)
            {
                Debug.LogError("[InputManager] 'Gameplay' action map not found in InputActionAsset");
                return;
            }

            gameplayMap.Enable();
            BindActions();
        }

        private void OnStateChanged(StateChangeArgs args)
        {
            currentState = args.NewState;
            ApplyContextFilter(currentState);
            das.Reset();
            // Fast-cursor is an InputManager input, not DAS state — clear it here, not inside Reset.
            IsFastCursorHeld = false;
        }

        private void ApplyContextFilter(GameState state)
        {
            if (inputContextTable == null)
            {
                Debug.LogError("[InputManager] Input Context Table is not wired — cannot filter input by state.");
                return;
            }

            var allowed = inputContextTable.GetAllowedActionNames(state);

            foreach (var action in gameplayMap.actions)
            {
                if (allowed.Contains(action.name))
                    action.Enable();
                else
                    action.Disable();
            }
        }

        private void OnInputActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is not InputAction action) return;

            var device = action.activeControl?.device;
            if (device == null) return;

            InputDeviceType newType;
            if (device is Keyboard) newType = InputDeviceType.Keyboard;
            else if (device is Gamepad) newType = InputDeviceType.Gamepad;
            else if (device is Mouse) newType = InputDeviceType.Mouse;
            else return;

            if (newType != ActiveDevice)
            {
                ActiveDevice = newType;
                OnDeviceChanged?.Invoke(newType);
            }
        }

        #region Action Bindings

        private void BindActions()
        {
            Bind("CursorUp",    _ => das.Press(CursorDirection.Up));
            Bind("CursorDown",  _ => das.Press(CursorDirection.Down));
            Bind("CursorLeft",  _ => das.Press(CursorDirection.Left));
            Bind("CursorRight", _ => das.Press(CursorDirection.Right));

            BindCancel("CursorUp",    _ => das.Release(CursorDirection.Up));
            BindCancel("CursorDown",  _ => das.Release(CursorDirection.Down));
            BindCancel("CursorLeft",  _ => das.Release(CursorDirection.Left));
            BindCancel("CursorRight", _ => das.Release(CursorDirection.Right));

            Bind("Confirm", _ => confirmPendingThisFrame = true);
            Bind("Cancel",  _ => cancelPendingThisFrame = true);

            Bind("FastCursor", _ => IsFastCursorHeld = true);
            BindCancel("FastCursor", _ => IsFastCursorHeld = false);

            Bind("OpenMapMenu",      _ => OnOpenMapMenu?.Invoke());
            Bind("OpenUnitInfo",     _ => OnOpenUnitInfo?.Invoke());
            Bind("ToggleMapOverlay", _ => OnToggleMapOverlay?.Invoke());
            Bind("Pause",            _ => OnPause?.Invoke());
            Bind("SkipAnimation",    _ => OnSkipAnimation?.Invoke());
            Bind("SkipDialogue",     _ => OnSkipDialogue?.Invoke());

            Bind("HoldAdvanceDialogue",       _ => OnHoldAdvanceDialogue?.Invoke(true));
            BindCancel("HoldAdvanceDialogue", _ => OnHoldAdvanceDialogue?.Invoke(false));

            Bind("NextUnit", _ => OnNextUnit?.Invoke());
            Bind("PrevUnit", _ => OnPrevUnit?.Invoke());

            Bind("PeekObjective",       _ => OnPeekObjective?.Invoke(true));
            BindCancel("PeekObjective", _ => OnPeekObjective?.Invoke(false));
        }

        private void Bind(string actionName, Action<InputAction.CallbackContext> callback)
        {
            var action = gameplayMap.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[InputManager] Action '{actionName}' not found in Gameplay map");
                return;
            }
            action.performed += callback;
            actionUnbinders.Add(() => action.performed -= callback);
        }

        private void BindCancel(string actionName, Action<InputAction.CallbackContext> callback)
        {
            var action = gameplayMap.FindAction(actionName);
            if (action == null) return;
            action.canceled += callback;
            actionUnbinders.Add(() => action.canceled -= callback);
        }

        private void UnbindActions()
        {
            foreach (var unbind in actionUnbinders)
                unbind();
            actionUnbinders.Clear();
        }

        // Whether an action is currently being held down. Used by callers that
        // need a modifier-key check at decision time (e.g. hold SkipAnimation
        // while confirming an attack to flip the playback speed for that one
        // combat).
        public bool IsActionHeld(GameInputAction gameAction) => IsActionHeld(gameAction.ToString());

        public bool IsActionHeld(string actionName)
        {
            var action = gameplayMap?.FindAction(actionName);
            return action != null && action.IsPressed();
        }

        #endregion

        // Cancel takes priority over Confirm when both fire on the same frame —
        // protects against accidental confirmations when the player meant to back out.
        private void ResolveSameFramePriority()
        {
            if (cancelPendingThisFrame)
            {
                OnCancel?.Invoke();
                confirmPendingThisFrame = false;
            }
            else if (confirmPendingThisFrame)
            {
                OnConfirm?.Invoke();
            }
        }
    }
}
