using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Hub
{
    // Leaves the hub for the battle the visit points at.
    //
    // Both of the spec's modes end up here. An automatic departure is the Depart action at the end
    // of a scripted sequence; a confirmed one is a conversation whose "yes" branch raises the
    // departure flag. Neither needs its own machinery — the conversation graph already does
    // confirmations, and routing both through one place means the checks can't be skipped by
    // taking the other road.
    //
    // Leaving is a handover, not a mode, so it holds the hub's lock rather than being a state. That
    // is also what lets a refused departure hand control back: a terminal state had nowhere to go.
    public sealed class HubDepartureController : MonoBehaviour
    {
        // The flag a confirmation conversation raises on its "leave" branch.
        public const string DepartFlag = "depart";

        [SerializeField] private HubEventRunner events;

        public bool HasDeparted { get; private set; }

        private void Awake()
        {
            if (events == null) events = FindFirstObjectByType<HubEventRunner>();
        }

        // Both roads in. A scripted sequence asks outright; a confirmation conversation raises the
        // flag on its "leave" branch, and this is the thing that knows what that flag means.
        private void OnEnable()
        {
            InteractionEvents.FlagRaised += OnFlagRaised;
            if (events == null) return;
            events.FlagRaised += OnFlagRaised;
            events.DepartureRequested += OnDepartureRequested;
        }

        private void OnDisable()
        {
            InteractionEvents.FlagRaised -= OnFlagRaised;
            if (events == null) return;
            events.FlagRaised -= OnFlagRaised;
            events.DepartureRequested -= OnDepartureRequested;
        }

        private void OnDepartureRequested(string _) => TryDepart();

        private void OnFlagRaised(string flagId)
        {
            if (flagId == DepartFlag) TryDepart();
        }

        public bool TryDepart()
        {
            if (HasDeparted) return false;

            HubProgressService progress = HubProgressService.Instance;
            if (progress == null) return false;

            string nextInCampaign = GameFlow.Instance != null ? GameFlow.Instance.NextBattleMapId : null;
            if (!DepartureGate.CanDepart(progress.CanDepart, progress.DestinationMapId, nextInCampaign, out string problem))
            {
                Debug.LogError($"[HubDeparture] Can't leave visit '{progress.Visit.VisitId}': {problem}.");
                return false;
            }

            return Commit(progress);
        }

        // Locked before the battle is asked for, so nothing can be pressed during the handover; and
        // only written off as departed once the state actually changed, so a battle that fails to
        // load leaves her standing in the hub rather than nowhere.
        private bool Commit(HubProgressService progress)
        {
            if (!HubControlGate.Instance.TryBeginHandover()) return false;

            GameFlow.Instance.NotifyHubVisitFinished();

            if (GameStateManager.Instance.CurrentState == GameState.HubExploration)
            {
                Debug.LogError($"[HubDeparture] Asked for '{progress.DestinationMapId}' but the game is still in the hub.");
                HubControlGate.Instance.EndHandover();
                return false;
            }

            // The lock is deliberately still held: the hub is going away, and nothing here should
            // answer another press on the way out.
            progress.State.MarkDeparted();
            HasDeparted = true;
            return true;
        }
    }
}
