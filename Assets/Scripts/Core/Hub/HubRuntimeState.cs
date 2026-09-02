using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;

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

    // Everything that has changed about the current visit since it loaded. Lives for the session
    // only — nothing is written to disk yet — but it is kept as plain serializable data with lists
    // instead of dictionaries and asset ids instead of asset references, so the eventual save
    // ticket is wiring rather than a rewrite.
    //
    // Lookups are linear because these collections hold a few dozen entries at most; a dictionary
    // would cost more in serialization awkwardness than it saves in time.
    [Serializable]
    public class HubRuntimeState : IDialogueMemory
    {
        [SerializeField] private string visitId;
        [SerializeField] private int objectiveIndex;

        // Targets cleared for the objective that is active right now. Reset on every advance, so a
        // player who talks to someone early can't bank credit for a stage that hasn't started.
        [SerializeField] private List<string> satisfiedTargets = new();

        [SerializeField] private List<string> completedObjectiveIds = new();
        [SerializeField] private List<string> completedConversationIds = new();
        [SerializeField] private List<string> askedTopics = new();
        [SerializeField] private List<string> completedEventIds = new();
        [SerializeField] private List<string> openGates = new();
        [SerializeField] private List<HubInteractableRecord> interactables = new();
        [SerializeField] private List<HubRelocationRecord> relocations = new();
        [SerializeField] private List<HubConversationRecord> conversationOverrides = new();
        [SerializeField] private bool departed;

        [SerializeField] private string currentLocationId;

        // Which of the six student houses she is currently inside. They share one interior, so the
        // name board, the occupant and the way back out all hang off this rather than off the room.
        [SerializeField] private string houseIdentity;

        // Where an exit with no authored destination sends her: back out of the door she came in by.
        [SerializeField] private string returnLocationId;
        [SerializeField] private Vector2 returnSpawn;
        [SerializeField] private Facing returnFacing;
        [SerializeField] private bool hasReturn;

        public string VisitId => visitId;
        public int ObjectiveIndex => objectiveIndex;
        public int SatisfiedTargetCount => satisfiedTargets.Count;
        public IReadOnlyList<string> SatisfiedTargets => satisfiedTargets;
        public IReadOnlyList<string> CompletedObjectiveIds => completedObjectiveIds;
        public IReadOnlyList<HubRelocationRecord> Relocations => relocations;
        public bool Departed => departed;

        public HubRuntimeState(string visitId)
        {
            this.visitId = visitId;
        }

        // Objective progress

        public bool HasSatisfied(string targetId) => satisfiedTargets.Contains(targetId);

        public bool Satisfy(string targetId)
        {
            if (satisfiedTargets.Contains(targetId)) return false;
            satisfiedTargets.Add(targetId);
            return true;
        }

        public void CompleteObjective(string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId) && !completedObjectiveIds.Contains(objectiveId))
                completedObjectiveIds.Add(objectiveId);
            satisfiedTargets.Clear();
            objectiveIndex++;
        }

        public bool IsObjectiveCompleted(string objectiveId) => completedObjectiveIds.Contains(objectiveId);

        // Conversations and events

        // --- IDialogueMemory ---
        // The visit already tracks both of these; these are the names the dialogue system asks by.
        public bool HasPlayed(string scriptId) => HasCompletedConversation(scriptId);
        public void MarkPlayed(string scriptId) => MarkConversationCompleted(scriptId);
        public bool HasChosen(string scriptId, string optionId) => HasAskedTopic(scriptId, optionId);
        public void MarkChosen(string scriptId, string optionId) => MarkTopicAsked(scriptId, optionId);

        public bool HasCompletedConversation(string conversationId) => completedConversationIds.Contains(conversationId);

        public void MarkConversationCompleted(string conversationId)
        {
            if (!string.IsNullOrEmpty(conversationId) && !completedConversationIds.Contains(conversationId))
                completedConversationIds.Add(conversationId);
        }

        // Which topics she has already raised, so a menu can grey them out and give a shorter answer
        // the second time. Scoped per conversation, because two characters can own a topic of the
        // same name without sharing whether it has been asked.
        public bool HasAskedTopic(string conversationId, string optionId) =>
            askedTopics.Contains(TopicKey(conversationId, optionId));

        public void MarkTopicAsked(string conversationId, string optionId)
        {
            string key = TopicKey(conversationId, optionId);
            if (!string.IsNullOrEmpty(optionId) && !askedTopics.Contains(key)) askedTopics.Add(key);
        }

        private static string TopicKey(string conversationId, string optionId) => conversationId + ":" + optionId;

        public bool HasCompletedEvent(string eventId) => completedEventIds.Contains(eventId);

        public void MarkEventCompleted(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId) && !completedEventIds.Contains(eventId))
                completedEventIds.Add(eventId);
        }

        // World state written by objective effects

        public bool IsGateOpen(string gateId) => openGates.Contains(gateId);

        public void SetGate(string gateId, bool open)
        {
            if (string.IsNullOrEmpty(gateId)) return;
            if (open && !openGates.Contains(gateId)) openGates.Add(gateId);
            else if (!open) openGates.Remove(gateId);
        }

        public HubInteractableState GetInteractableState(string interactableId, HubInteractableState fallback)
        {
            foreach (var record in interactables)
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
            foreach (var candidate in relocations)
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
            foreach (var record in conversationOverrides)
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

        public void MarkDeparted() => departed = true;

        // Where she is, and which house she is in if it is one of the shared interiors.

        public string CurrentLocationId => currentLocationId;
        public string HouseIdentity => houseIdentity;

        public void EnterLocation(string locationId, string identity)
        {
            currentLocationId = locationId;
            // An empty identity leaves the old one alone, so walking around inside a house doesn't
            // forget whose house it is.
            if (!string.IsNullOrEmpty(identity)) houseIdentity = identity;
        }

        public void RememberReturn(string locationId, Vector2 spawn, Facing facing)
        {
            returnLocationId = locationId;
            returnSpawn = spawn;
            returnFacing = facing;
            hasReturn = true;
        }

        public bool TryGetReturn(out string locationId, out Vector2 spawn, out Facing facing)
        {
            locationId = returnLocationId;
            spawn = returnSpawn;
            facing = returnFacing;
            return hasReturn;
        }
    }
}
