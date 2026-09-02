using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Works out what she could interact with each frame and acts on the press when it comes.
    //
    // Runs after the player controller so the reach check uses where she actually ended up this
    // frame, not where she was before moving.
    [DefaultExecutionOrder(50)]
    public sealed class HubInteractionDriver : MonoBehaviour
    {
        [SerializeField] private HubInputRouter router;
        [SerializeField] private HubActor player;

        private readonly List<HubInteractionCandidate> candidates = new();
        private readonly PromptHysteresis<string> prompt = new();
        private readonly Dictionary<string, HubInteractionCandidate> byId = new();

        // The target the prompt should be showing, or null. Fires only when it actually changes.
        public event Action<HubInteractionCandidate?> PromptChanged;

        // Raised once per accepted press. Phase 4 hangs conversations off this.
        public event Action<HubInteractionCandidate> Interacted;

        private string shownTargetId;

        public void Bind(HubInputRouter inputRouter, HubActor protagonist)
        {
            router = inputRouter;
            player = protagonist;
        }

        private void Update()
        {
            if (router == null || player == null) return;

            if (!router.Gate.AcceptsWorldInteraction)
            {
                ClearPrompt();
                return;
            }

            HubInteractionCandidate? target = ResolveTarget();
            PublishPrompt(target);

            if (router.InteractPressed && target.HasValue) Interacted?.Invoke(target.Value);
        }

        private HubInteractionCandidate? ResolveTarget()
        {
            GatherCandidates();

            string resolvedId = HubInteractionResolver.TryResolve(
                player.Position, player.Facing, candidates,
                HubLocationService.Instance?.Collision, out HubInteractionCandidate chosen)
                ? chosen.Id
                : null;

            // Held briefly past the moment it stops being valid, so standing on the edge of a
            // target's reach doesn't strobe the prompt.
            string settledId = prompt.Tick(resolvedId, Time.deltaTime);
            return settledId != null && byId.TryGetValue(settledId, out HubInteractionCandidate settled)
                ? settled
                : null;
        }

        private void GatherCandidates()
        {
            candidates.Clear();
            byId.Clear();

            foreach (HubActor actor in HubWorld.Actors)
            {
                if (actor == null || actor == player || string.IsNullOrEmpty(actor.ConversationId)) continue;
                Add(new HubInteractionCandidate(actor.CharacterId, HubTargetKind.Character,
                    actor.Position, HubVerb.Talk));
            }

            foreach (HubInteractable interactable in HubWorld.Interactables)
            {
                if (interactable == null || !interactable.IsPresent) continue;
                Add(interactable.ToCandidate());
            }

            GatherDoors();
        }

        // A shut door is still a candidate, so walking up to one gets an answer rather than
        // silence. Whether it opens is decided when the press lands.
        private void GatherDoors()
        {
            HubLocationData here = HubLocationService.Instance?.CurrentLocation;
            if (here == null) return;

            foreach (HubDoor door in here.Doors)
                Add(new HubInteractionCandidate(door.doorId, HubTargetKind.Door, door.position, door.verb));
        }

        private void Add(HubInteractionCandidate candidate)
        {
            candidates.Add(candidate);
            byId[candidate.Id] = candidate;
        }

        private void PublishPrompt(HubInteractionCandidate? target)
        {
            string id = target?.Id;
            if (id == shownTargetId) return;

            shownTargetId = id;
            PromptChanged?.Invoke(target);
        }

        private void ClearPrompt()
        {
            prompt.Clear();
            PublishPrompt(null);
        }
    }
}
