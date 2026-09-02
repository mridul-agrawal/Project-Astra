using System;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Hub
{
    // Answers one question for the whole hub: may anything here act right now?
    public class HubControlGate
    {
        // Read anywhere via HubControlGate.Instance, the same shape as HubLocationService — half
        // the hub asks this question and none of it should need a wire to whoever holds the lock.
        public static HubControlGate Instance { get; private set; }

        // The single set-point. HubBootstrapper calls it as a visit opens, so a new visit never
        // inherits a lock the last one was still holding.
        public static void Begin() => Instance = new HubControlGate();

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
                Debug.LogWarning("[HubControlGate] EndHandover with nothing in flight. Ignored.");
                return;
            }

            handoversInFlight--;
            if (handoversInFlight == 0) HandoverEnded?.Invoke();
        }
    }
}
