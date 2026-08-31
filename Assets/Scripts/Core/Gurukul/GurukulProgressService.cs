using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Single source of truth for "which visit are we in and how far through it are we". Loaded once
    // per visit by GurukulBootstrapper, then read anywhere via GurukulProgressService.Instance —
    // same shape as MapService.
    //
    // Session-scoped by design: a visit is a complete authored starting state, so loading one
    // deliberately throws away whatever the previous visit left behind.
    public class GurukulProgressService
    {
        public static GurukulProgressService Instance { get; private set; }

        private readonly GurukulVisitData visit;
        private readonly GurukulRuntimeState state;
        private readonly ObjectiveSequenceRunner objectives;

        private GurukulProgressService(GurukulVisitData visit)
        {
            this.visit = visit;
            state = new GurukulRuntimeState(visit.VisitId);
            objectives = new ObjectiveSequenceRunner(visit.Objectives, state);
            ApplyBaselineGates();
        }

        // The single set-point. GurukulBootstrapper calls this when a visit loads.
        public static void Load(GurukulVisitData visit)
        {
            Instance = visit != null ? new GurukulProgressService(visit) : null;
        }

        public GurukulVisitData Visit => visit;
        public GurukulRuntimeState State => state;
        public ObjectiveSequenceRunner Objectives => objectives;

        // Departure stays shut until every authored objective is done — the GDD's one hard rule
        // about leaving a visit.
        public bool CanDepart => objectives.IsVisitComplete;

        public string DestinationMapId => visit != null ? visit.Departure.destinationMapId : null;

        private void ApplyBaselineGates()
        {
            foreach (string gate in visit.OpenGates)
                state.SetGate(gate, true);

            foreach (GurukulInteractableOverride authored in visit.InteractableOverrides)
                state.SetInteractableState(authored.interactableId, authored.state);
        }

        // Where a character actually stands right now: their authored placement unless an objective
        // has since moved them.
        public bool TryGetPlacement(string characterId, out GurukulCharacterPlacement placement)
        {
            placement = default;
            bool found = false;

            foreach (GurukulCharacterPlacement authored in visit.CharacterPlacements)
            {
                if (authored.characterId != characterId) continue;
                placement = authored;
                found = true;
                break;
            }
            if (!found) return false;

            if (state.TryGetRelocation(characterId, out GurukulRelocationRecord moved))
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
