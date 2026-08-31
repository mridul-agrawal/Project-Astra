using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Gurukul
{
    // What the hub itself is doing, when the hub is the thing in control.
    public enum GurukulSubState
    {
        FreeExploration,
        ScriptedEvent,
        LocationTransition,
        Departure
    }

    // Validated state machine for the hub's remaining modes.
    //
    // Conversations and choices used to live here too. They are high-level GameStates now — a
    // conversation is GameState.Dialogue wherever it happens — so this machine no longer has an
    // opinion about them, and asks GameStateManager whether the hub is in control at all before
    // letting anything through.
    public class GurukulStateMachine
    {
        private static readonly HashSet<(GurukulSubState, GurukulSubState)> LegalTransitions = BuildLegalTransitions();

        private GurukulSubState currentState;

        // Fires with (previous, next) so listeners can react to the specific edge — returning to
        // FreeExploration from an event needs the interact button re-armed, arriving from a
        // location transition does not.
        public event Action<GurukulSubState, GurukulSubState> StateChanged;

        public GurukulStateMachine(GurukulSubState initialState = GurukulSubState.FreeExploration)
        {
            currentState = initialState;
        }

        public GurukulSubState CurrentState => currentState;

        public bool AcceptsMovement => IsHubInControl && currentState == GurukulSubState.FreeExploration;
        public bool AcceptsWorldInteraction => AcceptsMovement;

        // A conversation or a cutscene owns the moment even though the hub scene is still loaded,
        // so nothing hub-side may act. A null manager means someone pressed Play on the scene
        // directly, which should still be walkable.
        private static bool IsHubInControl =>
            GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentState == GameState.HubExploration;

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
                (GurukulSubState.FreeExploration, GurukulSubState.ScriptedEvent),
                (GurukulSubState.FreeExploration, GurukulSubState.LocationTransition),
                (GurukulSubState.FreeExploration, GurukulSubState.Departure),

                // Events move people between locations, and can run straight into the battle
                // without ever handing control back.
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
