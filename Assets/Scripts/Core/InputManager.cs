using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAstra.Core
{
    public enum InputDeviceType { Keyboard, Gamepad, Mouse }

    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        [Header("DAS Settings")]
        [SerializeField] private float _dasInitialDelay = 0.4f;
        [SerializeField] private float _dasRepeatRate = 0.1f;
        [SerializeField] private float _dasFastRepeatRate = 0.05f;

        // Logical action events — game systems subscribe to these
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

        // DAS state per direction
        private readonly float[] _dasTimers = new float[4];
        private readonly bool[] _dasInitialMoveFired = new bool[4];
        private readonly bool[] _directionHeld = new bool[4];

        // Confirm/Cancel same-frame priority
        private bool _confirmPendingThisFrame;
        private bool _cancelPendingThisFrame;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _gameplayMap = _inputActions.FindActionMap("Gameplay");
            if (_gameplayMap == null)
            {
                Debug.LogError("[InputManager] 'Gameplay' action map not found in InputActionAsset");
                return;
            }

            _gameplayMap.Enable();
            BindActions();
        }

        private void OnEnable()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Register(OnStateChanged);

            InputSystem.onActionChange += OnInputActionChange;
        }

        private void OnDisable()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Unregister(OnStateChanged);

            InputSystem.onActionChange -= OnInputActionChange;
        }

        private void Start()
        {
            _currentState = GameStateManager.Instance.CurrentState;
            ApplyContextFilter(_currentState);
        }

        private void Update()
        {
            UpdateDAS();
            ResolveSameFramePriority();
        }

        private void LateUpdate()
        {
            _confirmPendingThisFrame = false;
            _cancelPendingThisFrame = false;
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            _currentState = args.NewState;
            ApplyContextFilter(_currentState);
            ResetDAS();
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

        // Tracks which device was most recently used
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
            Bind("CursorUp", ctx => StartDirection(0));
            Bind("CursorDown", ctx => StartDirection(1));
            Bind("CursorLeft", ctx => StartDirection(2));
            Bind("CursorRight", ctx => StartDirection(3));

            BindCancel("CursorUp", _ => StopDirection(0));
            BindCancel("CursorDown", _ => StopDirection(1));
            BindCancel("CursorLeft", _ => StopDirection(2));
            BindCancel("CursorRight", _ => StopDirection(3));

            Bind("Confirm", _ => _confirmPendingThisFrame = true);
            Bind("Cancel", _ => _cancelPendingThisFrame = true);

            Bind("FastCursor", _ => IsFastCursorHeld = true);
            BindCancel("FastCursor", _ => IsFastCursorHeld = false);

            Bind("OpenMapMenu", _ => OnOpenMapMenu?.Invoke());
            Bind("OpenUnitInfo", _ => OnOpenUnitInfo?.Invoke());
            Bind("ToggleMapOverlay", _ => OnToggleMapOverlay?.Invoke());
            Bind("Pause", _ => OnPause?.Invoke());
            Bind("SkipAnimation", _ => OnSkipAnimation?.Invoke());
            Bind("SkipDialogue", _ => OnSkipDialogue?.Invoke());

            Bind("HoldAdvanceDialogue", _ => OnHoldAdvanceDialogue?.Invoke(true));
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

        #region DAS (Delayed Auto-Shift)

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,    // 0 = Up
            Vector2Int.down,  // 1 = Down
            Vector2Int.left,  // 2 = Left
            Vector2Int.right  // 3 = Right
        };

        private void StartDirection(int dir)
        {
            _directionHeld[dir] = true;
            _dasTimers[dir] = 0f;
            _dasInitialMoveFired[dir] = true;
            OnCursorMove?.Invoke(Directions[dir]);
        }

        private void StopDirection(int dir)
        {
            _directionHeld[dir] = false;
            _dasTimers[dir] = 0f;
            _dasInitialMoveFired[dir] = false;
        }

        private void UpdateDAS()
        {
            float repeatRate = IsFastCursorHeld ? _dasFastRepeatRate : _dasRepeatRate;

            for (int i = 0; i < 4; i++)
            {
                if (!_directionHeld[i]) continue;

                _dasTimers[i] += Time.deltaTime;

                if (_dasInitialMoveFired[i] && _dasTimers[i] < _dasInitialDelay)
                    continue;

                if (_dasTimers[i] >= (_dasInitialMoveFired[i] ? _dasInitialDelay : repeatRate))
                {
                    _dasTimers[i] = _dasInitialMoveFired[i] ? 0f : _dasTimers[i] - repeatRate;
                    _dasInitialMoveFired[i] = false;
                    OnCursorMove?.Invoke(Directions[i]);
                }
            }
        }

        private void ResetDAS()
        {
            for (int i = 0; i < 4; i++)
            {
                _directionHeld[i] = false;
                _dasTimers[i] = 0f;
                _dasInitialMoveFired[i] = false;
            }
            IsFastCursorHeld = false;
        }

        #endregion

        #region Same-Frame Priority

        // CANCEL takes priority over CONFIRM if both fire on the same frame
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

        #endregion
    }
}
