using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core.State
{
    // The game's top-level state machine. Only one GameState is active at a time, and only one
    // transition can land per frame so multiple scripts can't fight over the state in one tick.
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameStateTransitionTable _transitionTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private GameState _initialState = GameState.TitleScreen;

        private GameState _currentState;
        private GameState _menuReturnState;

        // First transition request in a frame wins; the rest are discarded.
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
                    $"already processed a transition this frame. Requester: {requesterName}");
                return false;
            }

            if (!_transitionTable.IsValid(_currentState, target))
            {
                Debug.LogError(
                    $"[GameStateManager] ILLEGAL transition: {_currentState} -> {target}. Requester: {requesterName}");
                return false;
            }

            if (IsContextMenu(target))
                _menuReturnState = _currentState;

            ExecuteTransition(target);
            _oneTransitionPerFrameGate = true;
            return true;
        }

        public bool ReturnFromContextMenu(string requester = null)
        {
            if (!IsContextMenu(_currentState))
            {
                Debug.LogError(
                    $"[GameStateManager] ReturnFromContextMenu called from invalid state: {_currentState}. " +
                    $"Requester: {requester ?? "unknown"}");
                return false;
            }

            return RequestTransition(_menuReturnState, requester);
        }

        // Bypasses the transition table. Use only for crash recovery or null-state fallback —
        // every call logs an error so unexpected forces stay loud and visible.
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

        private static bool IsContextMenu(GameState state) =>
            state == GameState.SaveMenu || state == GameState.SettingsMenu;

        #region Test helpers

        // Awake() doesn't run in EditMode tests, so this lets fixtures wire dependencies manually.
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

        #endregion
    }
}
