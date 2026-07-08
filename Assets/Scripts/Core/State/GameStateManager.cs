using System.Runtime.CompilerServices;
using UnityEngine;
using ProjectAstra.Core.Events;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core.State
{
    // The game's top-level state machine. Only one GameState is active at a time, and only one
    // transition can land per frame so multiple scripts can't fight over the state in one tick.
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        // Inspector References:
        [SerializeField] private GameStateTransitionTable transitionTable;
        [SerializeField] private GameState initialState = GameState.TitleScreen;

        // Private Variables:
        private GameState currentState;
        private GameState stateToReturnToWhenMenuCloses;
        private bool hasTransitionedThisFrame;

        // Properties:
        public GameState CurrentState => currentState;
        public GameState MenuReturnState => stateToReturnToWhenMenuCloses;

        private void Awake()
        {
            InitializeSingleton();
            transitionTable.Initialize();
            currentState = initialState;
        }

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate() => ResetFrameGate();

        public bool RequestTransition(GameState target, string requester = null)
        {
            if (IsBlockedThisFrame(target, requester)) return false;
            if (IsIllegalTransition(target, requester)) return false;

            RememberMenuReturnIfNeeded(target);
            ExecuteTransition(target);
            hasTransitionedThisFrame = true;
            return true;
        }

        private void ExecuteTransition(GameState target)
        {
            var previous = currentState;
            currentState = target;
            EventService.Instance?.RaiseGameStateChanged(previous, target);
        }

        // Returns true (and logs) if the requested move isn't in the transition table.
        private bool IsIllegalTransition(GameState target, string requester)
        {
            if (transitionTable.IsValid(currentState, target)) return false;
            Debug.LogError(
                $"[GameStateManager] ILLEGAL transition: {currentState} -> {target}. Requester: {RequesterName(requester)}");
            return true;
        }

        private static string RequesterName(string requester) => requester ?? "unknown";


        // Return From Menu Logic:
        // Called by Context Menu UI Scripts to transition back to previous game states:
        public bool ReturnFromContextMenu(string requester = null)
        {
            if (!IsThisStateContextMenu(currentState))
            {
                LogInvalidContextMenuReturn(requester);
                return false;
            }

            return RequestTransition(stateToReturnToWhenMenuCloses, requester);
        }

        private void RememberMenuReturnIfNeeded(GameState target)
        {
            if (IsThisStateContextMenu(target))
                stateToReturnToWhenMenuCloses = currentState;
        }

        private static bool IsThisStateContextMenu(GameState state) => state == GameState.SaveMenu || state == GameState.SettingsMenu;

        private void LogInvalidContextMenuReturn(string requester) => Debug.LogError(
                $"[GameStateManager] ReturnFromContextMenu called from invalid state: {currentState}. " +
                $"Requester: {RequesterName(requester)}");


        // Frame Gate Logic:
        private bool IsBlockedThisFrame(GameState target, string requester)
        {
            if (!hasTransitionedThisFrame) return false;
            Debug.LogWarning(
                $"[GameStateManager] Transition to {target} discarded — " +
                $"already processed a transition this frame. Requester: {RequesterName(requester)}");
            return true;
        }

        internal void ResetFrameGate() => hasTransitionedThisFrame = false;

        #region Test Support

        // Bypasses the transition table. Use only for crash recovery or null-state fallback —
        // every call logs an error so unexpected forces stay loud and visible.
        public void ForceState(GameState state, string reason)
        {
            Debug.LogError($"[GameStateManager] FORCED state change to {state}. Reason: {reason}");
            ExecuteTransition(state);
        }


        // Awake() doesn't run in EditMode tests, so this lets fixtures wire dependencies manually.
        internal void Initialize(GameStateTransitionTable transitionTable, GameState initialState)
        {
            this.transitionTable = transitionTable;
            this.initialState = initialState;

            Instance = this;
            this.transitionTable.Initialize();
            currentState = this.initialState;
            hasTransitionedThisFrame = false;
        }

        #endregion

    }
}
