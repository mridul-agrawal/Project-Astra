using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ProjectAstra.Core.Events;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core.State
{
    // The game's top-level state machine. Only one GameState is active at a time, and only one
    // transition is ever mid-flight — but a move asked for while another is still being announced
    // is queued rather than dropped, so a chain like "the conversation ended, now the event that
    // owns it takes control back" survives landing in a single frame.
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        // A runaway A→B→A loop would hang the editor, so the drain gives up and says so.
        private const int MaxQueuedTransitionsPerDrain = 32;

        // States that hand control back to whoever opened them. Everything else is reached by an
        // explicit transition and so has no caller worth remembering.
        private static readonly HashSet<GameState> StatesThatReturnToCaller = new()
        {
            GameState.SaveMenu,
            GameState.SettingsMenu,
            GameState.Dialogue,
        };

        // Inspector References:
        [SerializeField] private GameStateTransitionTable transitionTable;
        [SerializeField] private GameState initialState = GameState.TitleScreen;

        // Private Variables:
        private readonly Queue<PendingTransition> queuedTransitions = new();
        private readonly Stack<GameState> callers = new();
        private GameState currentState;
        private bool isAnnouncing;

        // Properties:
        public GameState CurrentState => currentState;

        // Who the current state will hand control back to, for anyone who needs to show it.
        public GameState CallerState => callers.Count > 0 ? callers.Peek() : currentState;

        private readonly struct PendingTransition
        {
            public readonly GameState Target;
            public readonly string Requester;

            public PendingTransition(GameState target, string requester)
            {
                Target = target;
                Requester = requester;
            }
        }

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

        public bool RequestTransition(GameState target, string requester = null)
        {
            if (isAnnouncing) return QueueUntilAnnouncementEnds(target, requester);
            if (IsIllegalTransition(target, requester)) return false;

            ExecuteTransition(target);
            DrainQueuedTransitions();
            return true;
        }

        // A listener reacting to one state change by asking for another can't be served straight
        // away — the state it would be leaving is still being announced. It goes in the queue and
        // is judged against the table when its turn comes, not now.
        private bool QueueUntilAnnouncementEnds(GameState target, string requester)
        {
            queuedTransitions.Enqueue(new PendingTransition(target, requester));
            return true;
        }

        private void DrainQueuedTransitions()
        {
            int drained = 0;
            while (queuedTransitions.Count > 0)
            {
                if (++drained > MaxQueuedTransitionsPerDrain)
                {
                    LogRunawayQueue();
                    return;
                }

                PendingTransition next = queuedTransitions.Dequeue();
                if (IsIllegalTransition(next.Target, next.Requester)) continue;
                ExecuteTransition(next.Target);
            }
        }

        private void ExecuteTransition(GameState target)
        {
            GameState previous = currentState;
            UpdateCallerStack(leaving: previous, entering: target);
            currentState = target;
            Announce(previous, target);
        }

        private void Announce(GameState previous, GameState target)
        {
            isAnnouncing = true;
            try { EventService.Instance?.RaiseGameStateChanged(previous, target); }
            finally { isAnnouncing = false; }
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

        private void LogRunawayQueue() => Debug.LogError(
            $"[GameStateManager] Transition queue never settled after {MaxQueuedTransitionsPerDrain} moves — " +
            $"dropping {queuedTransitions.Count} more. Something is transitioning in a loop.");


        // Return To Caller Logic:
        // Dialogue, the save menu and the settings menu are all opened from somewhere and go back
        // there when they close. The stack is what lets dialogue opened from the hub return to the
        // hub and dialogue opened mid-battle return to the battle, with no caller hardcoding it.
        public bool ReturnToCaller(string requester = null)
        {
            if (!CanReturnToCaller())
            {
                LogInvalidReturn(requester);
                return false;
            }

            return RequestTransition(callers.Peek(), requester);
        }

        public bool CanReturnToCaller() =>
            callers.Count > 0 && StatesThatReturnToCaller.Contains(currentState);

        private void UpdateCallerStack(GameState leaving, GameState entering)
        {
            // Popped on the way out however the state is left, so a dialogue that ends in a game
            // over doesn't leave its caller behind for the next one to inherit.
            if (StatesThatReturnToCaller.Contains(leaving) && callers.Count > 0) callers.Pop();
            if (StatesThatReturnToCaller.Contains(entering)) callers.Push(leaving);
        }

        private void LogInvalidReturn(string requester) => Debug.LogError(
            $"[GameStateManager] ReturnToCaller called from {currentState}, which has no caller to return to. " +
            $"Requester: {RequesterName(requester)}");

        #region Test Support

        // Bypasses the transition table. Use only for crash recovery or null-state fallback —
        // every call logs an error so unexpected forces stay loud and visible.
        public void ForceState(GameState state, string reason)
        {
            Debug.LogError($"[GameStateManager] FORCED state change to {state}. Reason: {reason}");
            ExecuteTransition(state);
            DrainQueuedTransitions();
        }


        // Awake() doesn't run in EditMode tests, so this lets fixtures wire dependencies manually.
        internal void Initialize(GameStateTransitionTable transitionTable, GameState initialState)
        {
            this.transitionTable = transitionTable;
            this.initialState = initialState;

            Instance = this;
            this.transitionTable.Initialize();
            currentState = this.initialState;
            queuedTransitions.Clear();
            callers.Clear();
            isAnnouncing = false;
        }

        #endregion

    }
}
