using System;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Gurukul
{
    // Answers one question for the whole hub: may anything here act right now?
    //
    // Conversations and scripted sequences used to be states in here; they are high-level
    // GameStates now, so this asks GameStateManager about those. Walking through a doorway and
    // leaving for the battle used to be states too, but neither is a mode the player is *in* —
    // each is a handover in flight, so each is a lock that is held while it runs.
    public class GurukulControlGate
    {
        private int handoversInFlight;

        // Fires when the last handover finishes, so a button still held from before it started
        // can be re-armed rather than read the moment control comes back.
        public event Action HandoverEnded;

        public bool IsHandoverInFlight => handoversInFlight > 0;

        public bool AcceptsMovement => IsHubInControl && !IsHandoverInFlight;
        public bool AcceptsWorldInteraction => AcceptsMovement;

        // A conversation or a scripted sequence owns the moment even though the hub scene is still
        // loaded, so nothing hub-side may act. A null manager means someone pressed Play on the
        // scene directly, which should still be walkable.
        private static bool IsHubInControl =>
            GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentState == GameState.HubExploration;

        // Refused rather than nested: two doorways at once, or a door during a departure, would
        // each land the player somewhere the other didn't expect.
        public bool TryBeginHandover()
        {
            if (IsHandoverInFlight) return false;
            handoversInFlight++;
            return true;
        }

        public void EndHandover()
        {
            if (handoversInFlight == 0)
            {
                Debug.LogWarning("[GurukulControlGate] EndHandover with nothing in flight. Ignored.");
                return;
            }

            handoversInFlight--;
            if (handoversInFlight == 0) HandoverEnded?.Invoke();
        }
    }
}
