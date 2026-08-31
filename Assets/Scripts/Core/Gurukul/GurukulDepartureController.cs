using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Gurukul
{
    // Leaves the hub for the battle the visit points at.
    //
    // Both of the spec's modes end up here. An automatic departure is the Depart action at the end
    // of a scripted event; a confirmed one is a conversation whose "yes" branch raises the departure
    // flag. Neither needs its own machinery — the conversation graph already does confirmations, and
    // routing both through one place means the checks can't be skipped by taking the other road.
    public sealed class GurukulDepartureController : MonoBehaviour
    {
        // The flag a confirmation conversation raises on its "leave" branch.
        public const string DepartFlag = "depart";

        [SerializeField] private GurukulInputRouter router;

        public bool HasDeparted { get; private set; }

        public bool TryDepart()
        {
            if (HasDeparted) return false;

            GurukulProgressService progress = GurukulProgressService.Instance;
            if (progress == null) return false;

            string nextInCampaign = GameFlow.Instance != null ? GameFlow.Instance.NextBattleMapId : null;
            if (!DepartureGate.CanDepart(progress.CanDepart, progress.DestinationMapId, nextInCampaign, out string problem))
            {
                Debug.LogError($"[GurukulDeparture] Can't leave visit '{progress.Visit.VisitId}': {problem}.");
                return false;
            }

            return Commit(progress);
        }

        // Locked before the battle is asked for, so nothing can be pressed during the handover; and
        // only written off as departed once the state actually changed, so a battle that fails to
        // load leaves her standing in the hub rather than nowhere.
        private bool Commit(GurukulProgressService progress)
        {
            router.States.TryTransition(GurukulSubState.Departure);
            GameFlow.Instance.NotifyHubVisitFinished();

            if (GameStateManager.Instance.CurrentState == GameState.HubExploration)
            {
                Debug.LogError($"[GurukulDeparture] Asked for '{progress.DestinationMapId}' but the game is still in the hub.");
                router.States.TryTransition(GurukulSubState.FreeExploration);
                return false;
            }

            progress.State.MarkDeparted();
            HasDeparted = true;
            return true;
        }
    }
}
