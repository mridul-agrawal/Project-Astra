using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Single source of truth for which visit she is in and what has changed about it since.
    public class HubVisitService
    {
        public static HubVisitService Instance { get; private set; }

        private readonly HubVisitData visit;

        private HubVisitService(HubVisitData visit)
        {
            this.visit = visit;
            ApplyBaseline();
        }

        // The single set-point. HubBootstrapper calls this when a visit loads.
        public static void Load(HubVisitData visit)
        {
            Instance = visit != null ? new HubVisitService(visit) : null;
        }

        public HubVisitData Visit => visit;

        public HubDialogueMemory Dialogue { get; } = new();
        public HubWorldFlags Flags { get; } = new();
        public HubEventLedger Events { get; } = new();
        public HubLocationState Location { get; } = new();

        public bool Departed { get; private set; }
        public void MarkDeparted() => Departed = true;

        public string DestinationMapId => visit != null ? visit.Departure.destinationMapId : null;

        private void ApplyBaseline()
        {
            foreach (string gate in visit.OpenGates)
                Flags.SetGate(gate, true);

            foreach (HubInteractableOverride authored in visit.InteractableOverrides)
                Flags.SetInteractableState(authored.interactableId, authored.state);
        }

        // Where a character actually stands right now: their authored placement unless something
        // has since moved them.
        public bool TryGetPlacement(string characterId, out HubCharacterPlacement placement)
        {
            placement = default;
            if (!TryGetAuthored(characterId, out placement)) return false;

            if (Flags.TryGetRelocation(characterId, out HubRelocationRecord moved))
            {
                placement.locationId = moved.locationId;
                placement.position = moved.position;
                placement.facing = moved.facing;
            }

            string overridden = Flags.GetConversationOverride(characterId);
            if (!string.IsNullOrEmpty(overridden)) placement.conversationId = overridden;
            return true;
        }

        private bool TryGetAuthored(string characterId, out HubCharacterPlacement placement)
        {
            foreach (HubCharacterPlacement authored in visit.CharacterPlacements)
            {
                if (authored.characterId != characterId) continue;
                placement = authored;
                return true;
            }
            placement = default;
            return false;
        }
    }
}
