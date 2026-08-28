using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // The hub is always in exactly one of these. Append only.
    public enum GurukulSubState
    {
        FreeExploration,
        Conversation,
        ChoiceOrQuiz,
        ScriptedEvent,
        LocationTransition,
        Departure
    }

    // Validated state machine for the hub's six player-facing modes. GurukulInputRouter routes every
    // press through the current state, so a press can only ever mean one thing.
    //
    // Deliberately plain C# with a code-side transition set rather than a ScriptableObject — the
    // same call CursorStateMachine makes, and for the same reason. These are also kept off the
    // top-level GameState machine, which allows only one transition per frame and maps every state
    // to a scene by name; six more states there would break chained transitions and bloat three
    // shared assets.
    public class GurukulStateMachine
    {
        private static readonly HashSet<(GurukulSubState, GurukulSubState)> LegalTransitions = BuildLegalTransitions();

        private GurukulSubState currentState;

        // Fires with (previous, next) so listeners can react to the specific edge — returning to
        // FreeExploration from a conversation needs the interact button re-armed, arriving from a
        // location transition does not.
        public event Action<GurukulSubState, GurukulSubState> StateChanged;

        public GurukulStateMachine(GurukulSubState initialState = GurukulSubState.FreeExploration)
        {
            currentState = initialState;
        }

        public GurukulSubState CurrentState => currentState;

        public bool AcceptsMovement => currentState == GurukulSubState.FreeExploration;
        public bool AcceptsWorldInteraction => currentState == GurukulSubState.FreeExploration;

        public bool TryTransition(GurukulSubState next)
        {
            if (next == currentState) return true;

            if (!LegalTransitions.Contains((currentState, next)))
            {
                Debug.LogWarning($"[GurukulStateMachine] Illegal transition: {currentState} -> {next}. Rejected.");
                return false;
            }

            GurukulSubState previous = currentState;
            currentState = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }

        internal static bool IsLegal(GurukulSubState from, GurukulSubState to) =>
            from == to || LegalTransitions.Contains((from, to));

        private static HashSet<(GurukulSubState, GurukulSubState)> BuildLegalTransitions()
        {
            return new HashSet<(GurukulSubState, GurukulSubState)>
            {
                // Walking around is where everything starts from.
                (GurukulSubState.FreeExploration, GurukulSubState.Conversation),
                (GurukulSubState.FreeExploration, GurukulSubState.ScriptedEvent),
                (GurukulSubState.FreeExploration, GurukulSubState.LocationTransition),
                (GurukulSubState.FreeExploration, GurukulSubState.Departure),

                // A conversation can put up choices, hand off to an event, or just end.
                (GurukulSubState.Conversation, GurukulSubState.ChoiceOrQuiz),
                (GurukulSubState.Conversation, GurukulSubState.ScriptedEvent),
                (GurukulSubState.Conversation, GurukulSubState.FreeExploration),

                // A choice returns to its response, ends the conversation, starts what it triggered,
                // or confirms a departure.
                (GurukulSubState.ChoiceOrQuiz, GurukulSubState.Conversation),
                (GurukulSubState.ChoiceOrQuiz, GurukulSubState.FreeExploration),
                (GurukulSubState.ChoiceOrQuiz, GurukulSubState.ScriptedEvent),
                (GurukulSubState.ChoiceOrQuiz, GurukulSubState.Departure),

                // Events talk, move people between locations, and can run straight into the battle
                // without ever handing control back.
                (GurukulSubState.ScriptedEvent, GurukulSubState.Conversation),
                (GurukulSubState.ScriptedEvent, GurukulSubState.LocationTransition),
                (GurukulSubState.ScriptedEvent, GurukulSubState.FreeExploration),
                (GurukulSubState.ScriptedEvent, GurukulSubState.Departure),

                // A doorway lands in the new room, or straight into the event waiting there.
                (GurukulSubState.LocationTransition, GurukulSubState.FreeExploration),
                (GurukulSubState.LocationTransition, GurukulSubState.ScriptedEvent),

                // Departure is terminal: once it commits, the hub is done and the battle loads.
            };
        }
    }
}
