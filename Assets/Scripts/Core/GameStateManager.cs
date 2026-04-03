using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameStateTransitionTable _transitionTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private GameState _initialState = GameState.TitleScreen;

        private GameState _currentState;
        private bool _transitionProcessedThisFrame;
        private GameState _returnContext;

        private readonly Dictionary<GameState, Action> _entryHooks = new();
        private readonly Dictionary<GameState, Action> _exitHooks = new();

        public GameState CurrentState => _currentState;
        public GameState ReturnContext => _returnContext;

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
            _transitionProcessedThisFrame = false;
        }

        public bool RequestTransition(GameState target, string requester = null)
        {
            string requesterName = requester ?? "unknown";

            if (_transitionProcessedThisFrame)
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
                _returnContext = _currentState;
            }

            ExecuteTransition(target);
            _transitionProcessedThisFrame = true;
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

            return RequestTransition(_returnContext, requester);
        }

        // Bypasses transition table — only for crash recovery or null-state fallback
        public void ForceState(GameState state, string reason)
        {
            Debug.LogError($"[GameStateManager] FORCED state change to {state}. Reason: {reason}");
            ExecuteTransition(state);
        }

        public void RegisterEntryHook(GameState state, Action hook)
        {
            if (!_entryHooks.ContainsKey(state))
                _entryHooks[state] = hook;
            else
                _entryHooks[state] += hook;
        }

        public void RegisterExitHook(GameState state, Action hook)
        {
            if (!_exitHooks.ContainsKey(state))
                _exitHooks[state] = hook;
            else
                _exitHooks[state] += hook;
        }

        public void UnregisterEntryHook(GameState state, Action hook)
        {
            if (_entryHooks.ContainsKey(state))
                _entryHooks[state] -= hook;
        }

        public void UnregisterExitHook(GameState state, Action hook)
        {
            if (_exitHooks.ContainsKey(state))
                _exitHooks[state] -= hook;
        }

        private void ExecuteTransition(GameState target)
        {
            var previous = _currentState;

            if (_exitHooks.TryGetValue(previous, out var exitHook))
                exitHook?.Invoke();

            _currentState = target;

            if (_entryHooks.TryGetValue(target, out var entryHook))
                entryHook?.Invoke();

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
            _transitionProcessedThisFrame = false;
            _entryHooks.Clear();
            _exitHooks.Clear();
        }

        internal void ResetFrameGate()
        {
            _transitionProcessedThisFrame = false;
        }
    }
}
