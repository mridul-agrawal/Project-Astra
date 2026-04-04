using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core
{
    /// <summary>
    /// Singleton state machine that owns the current GameState, validates transitions, and enforces one-transition-per-frame.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameStateTransitionTable _transitionTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private GameState _initialState = GameState.TitleScreen;

        private GameState _currentState;
        private GameState _menuReturnState;

        // Prevents multiple transitions in one frame — first request wins, rest are discarded
        private bool _oneTransitionPerFrameGate;

        public GameState CurrentState => _currentState;
        public GameState MenuReturnState => _menuReturnState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _transitionTable.Initialize();
            _currentState = _initialState;
        }

        private void LateUpdate()
        {
            _oneTransitionPerFrameGate = false;
        }

        public bool RequestTransition(GameState target, string requester = null)
        {
            string requesterName = requester ?? "unknown";

            if (_oneTransitionPerFrameGate)
            {
                Debug.LogWarning(
                    $"[GameStateManager] Transition to {target} discarded — " +
                    $"transition already processed this frame. Requester: {requesterName}");
                return false;
            }

            if (!_transitionTable.IsValid(_currentState, target))
            {
                Debug.LogError(
                    $"[GameStateManager] ILLEGAL transition: {_currentState} -> {target}. " +
                    $"Requester: {requesterName}");
                return false;
            }

            if (target == GameState.SaveMenu || target == GameState.SettingsMenu)
            {
                _menuReturnState = _currentState;
            }

            ExecuteTransition(target);
            _oneTransitionPerFrameGate = true;
            return true;
        }

        public bool ReturnFromContextMenu(string requester = null)
        {
            if (_currentState != GameState.SaveMenu && _currentState != GameState.SettingsMenu)
            {
                Debug.LogError(
                    $"[GameStateManager] ReturnFromContextMenu called from invalid state: {_currentState}. " +
                    $"Requester: {requester ?? "unknown"}");
                return false;
            }

            return RequestTransition(_menuReturnState, requester);
        }

        // Bypasses transition table — only for crash recovery or null-state fallback
        public void ForceState(GameState state, string reason)
        {
            Debug.LogError($"[GameStateManager] FORCED state change to {state}. Reason: {reason}");
            ExecuteTransition(state);
        }

        private void ExecuteTransition(GameState target)
        {
            var previous = _currentState;
            _currentState = target;

            _stateChangedChannel?.Raise(new GameStateEventChannel.StateChangeArgs
            {
                PreviousState = previous,
                NewState = target
            });
        }

        // Awake() does not run in EditMode tests, so this provides manual init
        internal void Initialize(GameStateTransitionTable transitionTable, GameStateEventChannel eventChannel, GameState initialState)
        {
            _transitionTable = transitionTable;
            _stateChangedChannel = eventChannel;
            _initialState = initialState;

            Instance = this;
            _transitionTable.Initialize();
            _currentState = _initialState;
            _oneTransitionPerFrameGate = false;
        }

        internal void ResetFrameGate()
        {
            _oneTransitionPerFrameGate = false;
        }
    }
}
