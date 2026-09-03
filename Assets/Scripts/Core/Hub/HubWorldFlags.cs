using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    [Serializable]
    public struct HubInteractableRecord
    {
        public string interactableId;
        public HubInteractableState state;
    }

    [Serializable]
    public struct HubRelocationRecord
    {
        public string characterId;
        public string locationId;
        public Vector2 position;
        public Facing facing;
    }

    [Serializable]
    public struct HubConversationRecord
    {
        public string characterId;
        public string conversationId;
    }

    // What has changed about the world this visit: gates, objects, and who has moved.
    //
    // Written by quest events and scripted sequences, read by whatever the change is about. Nothing
    // in here decides what a change means — a door opening on a flag is the door's business.
    [Serializable]
    public class HubWorldFlags
    {
        [SerializeField] private List<string> openGates = new();
        [SerializeField] private List<HubInteractableRecord> interactables = new();
        [SerializeField] private List<HubRelocationRecord> relocations = new();
        [SerializeField] private List<HubConversationRecord> conversationOverrides = new();

        public IReadOnlyList<HubRelocationRecord> Relocations => relocations;

        public bool IsGateOpen(string gateId) => openGates.Contains(gateId);

        public void SetGate(string gateId, bool open)
        {
            if (string.IsNullOrEmpty(gateId)) return;
            if (open && !openGates.Contains(gateId)) openGates.Add(gateId);
            else if (!open) openGates.Remove(gateId);
        }

        public HubInteractableState GetInteractableState(string interactableId, HubInteractableState fallback)
        {
            foreach (HubInteractableRecord record in interactables)
                if (record.interactableId == interactableId) return record.state;
            return fallback;
        }

        public void SetInteractableState(string interactableId, HubInteractableState state)
        {
            if (string.IsNullOrEmpty(interactableId)) return;

            for (int i = 0; i < interactables.Count; i++)
            {
                if (interactables[i].interactableId != interactableId) continue;
                interactables[i] = new HubInteractableRecord { interactableId = interactableId, state = state };
                return;
            }
            interactables.Add(new HubInteractableRecord { interactableId = interactableId, state = state });
        }

        public bool TryGetRelocation(string characterId, out HubRelocationRecord record)
        {
            foreach (HubRelocationRecord candidate in relocations)
            {
                if (candidate.characterId != characterId) continue;
                record = candidate;
                return true;
            }
            record = default;
            return false;
        }

        public void Relocate(string characterId, string locationId, Vector2 position, Facing facing)
        {
            if (string.IsNullOrEmpty(characterId)) return;

            var record = new HubRelocationRecord
            {
                characterId = characterId, locationId = locationId, position = position, facing = facing
            };

            for (int i = 0; i < relocations.Count; i++)
            {
                if (relocations[i].characterId != characterId) continue;
                relocations[i] = record;
                return;
            }
            relocations.Add(record);
        }

        public string GetConversationOverride(string characterId)
        {
            foreach (HubConversationRecord record in conversationOverrides)
                if (record.characterId == characterId) return record.conversationId;
            return null;
        }

        public void SetConversationOverride(string characterId, string conversationId)
        {
            if (string.IsNullOrEmpty(characterId)) return;

            var record = new HubConversationRecord { characterId = characterId, conversationId = conversationId };
            for (int i = 0; i < conversationOverrides.Count; i++)
            {
                if (conversationOverrides[i].characterId != characterId) continue;
                conversationOverrides[i] = record;
                return;
            }
            conversationOverrides.Add(record);
        }
    }
}
