using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAstra.Core.State;

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

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        [Header("DAS Settings")]
        [SerializeField] private float _dasInitialDelay = 0.4f;
        [SerializeField] private float _dasRepeatRate = 0.1f;
        [SerializeField] private float _dasFastRepeatRate = 0.05f;

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
        public event Action<InputDeviceType> OnDeviceChanged;
        public event Action OnGamepadDisconnected;

        public InputDeviceType ActiveDevice { get; private set; } = InputDeviceType.Keyboard;
        public bool IsFastCursorHeld { get; private set; }

        private InputActionMap _gameplayMap;
        private GameState _currentState;
        private DelayedAutoShift _das;

        private bool _confirmPendingThisFrame;
        private bool _cancelPendingThisFrame;

        private void Awake()
        {
            CreateSingleton();
            CreateDelayedAutoShift();
            InitializeInputActionMap();
        }

        private void CreateDelayedAutoShift()
        {
            _das = new DelayedAutoShift(_dasInitialDelay, _dasRepeatRate, _dasFastRepeatRate);
            _das.CursorMoveTriggered += direction => OnCursorMove?.Invoke(direction);
        }

        private void OnEnable()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Register(OnStateChanged);

            InputSystem.onActionChange += OnInputActionChange;
        }

        private void Start()
        {
            _currentState = GameStateManager.Instance.CurrentState;
            ApplyContextFilter(_currentState);
        }

        private void OnDisable()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Unregister(OnStateChanged);

            InputSystem.onActionChange -= OnInputActionChange;
        }

        private void Update()
        {
            _das.Tick(Time.deltaTime, IsFastCursorHeld);
            ResolveSameFramePriority();
        }

        private void LateUpdate()
        {
            _confirmPendingThisFrame = false;
            _cancelPendingThisFrame = false;
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
            _gameplayMap = _inputActions.FindActionMap("Gameplay");
            if (_gameplayMap == null)
            {
                Debug.LogError("[InputManager] 'Gameplay' action map not found in InputActionAsset");
                return;
            }

            _gameplayMap.Enable();
            BindActions();
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            _currentState = args.NewState;
            ApplyContextFilter(_currentState);
            _das.Reset();
            // Fast-cursor is an InputManager input, not DAS state — clear it here, not inside Reset.
            IsFastCursorHeld = false;
        }

        private void ApplyContextFilter(GameState state)
        {
            var allowed = InputContext.GetAllowedActions(state);

            foreach (var action in _gameplayMap.actions)
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
            Bind("CursorUp",    _ => _das.Press(CursorDirection.Up));
            Bind("CursorDown",  _ => _das.Press(CursorDirection.Down));
            Bind("CursorLeft",  _ => _das.Press(CursorDirection.Left));
            Bind("CursorRight", _ => _das.Press(CursorDirection.Right));

            BindCancel("CursorUp",    _ => _das.Release(CursorDirection.Up));
            BindCancel("CursorDown",  _ => _das.Release(CursorDirection.Down));
            BindCancel("CursorLeft",  _ => _das.Release(CursorDirection.Left));
            BindCancel("CursorRight", _ => _das.Release(CursorDirection.Right));

            Bind("Confirm", _ => _confirmPendingThisFrame = true);
            Bind("Cancel",  _ => _cancelPendingThisFrame = true);

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
        }

        private void Bind(string actionName, Action<InputAction.CallbackContext> callback)
        {
            var action = _gameplayMap.FindAction(actionName);
            if (action != null) action.performed += callback;
            else Debug.LogWarning($"[InputManager] Action '{actionName}' not found in Gameplay map");
        }

        private void BindCancel(string actionName, Action<InputAction.CallbackContext> callback)
        {
            var action = _gameplayMap.FindAction(actionName);
            if (action != null) action.canceled += callback;
        }

        #endregion

        // Cancel takes priority over Confirm when both fire on the same frame —
        // protects against accidental confirmations when the player meant to back out.
        private void ResolveSameFramePriority()
        {
            if (_cancelPendingThisFrame)
            {
                OnCancel?.Invoke();
                _confirmPendingThisFrame = false;
            }
            else if (_confirmPendingThisFrame)
            {
                OnConfirm?.Invoke();
            }
        }
    }
}
