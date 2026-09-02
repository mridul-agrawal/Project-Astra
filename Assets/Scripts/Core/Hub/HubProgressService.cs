using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Single source of truth for which visit she is in and how far through it she is.
    public class HubProgressService
    {
        public static HubProgressService Instance { get; private set; }

        private readonly HubVisitData visit;
        private readonly HubRuntimeState state;
        private readonly ObjectiveSequenceRunner objectives;

        private HubProgressService(HubVisitData visit)
        {
            this.visit = visit;
            state = new HubRuntimeState(visit.VisitId);
            objectives = new ObjectiveSequenceRunner(visit.Objectives, state);
            ApplyBaselineGates();
        }

        // The single set-point. HubBootstrapper calls this when a visit loads.
        public static void Load(HubVisitData visit)
        {
            Instance = visit != null ? new HubProgressService(visit) : null;
        }

        public HubVisitData Visit => visit;
        public HubRuntimeState State => state;
        public ObjectiveSequenceRunner Objectives => objectives;

        // Departure stays shut until every authored objective is done — the GDD's one hard rule
        // about leaving a visit.
        public bool CanDepart => objectives.IsVisitComplete;

        public string DestinationMapId => visit != null ? visit.Departure.destinationMapId : null;

        private void ApplyBaselineGates()
        {
            foreach (string gate in visit.OpenGates)
                state.SetGate(gate, true);

            foreach (HubInteractableOverride authored in visit.InteractableOverrides)
                state.SetInteractableState(authored.interactableId, authored.state);
        }

        // Where a character actually stands right now: their authored placement unless an objective
        // has since moved them.
        public bool TryGetPlacement(string characterId, out HubCharacterPlacement placement)
        {
            placement = default;
            bool found = false;

            foreach (HubCharacterPlacement authored in visit.CharacterPlacements)
            {
                if (authored.characterId != characterId) continue;
                placement = authored;
                found = true;
                break;
            }
            if (!found) return false;

            if (state.TryGetRelocation(characterId, out HubRelocationRecord moved))
            {
                placement.locationId = moved.locationId;
                placement.position = moved.position;
                placement.facing = moved.facing;
            }

            string overridden = state.GetConversationOverride(characterId);
            if (!string.IsNullOrEmpty(overridden)) placement.conversationId = overridden;

            return true;
        }
    }
}
