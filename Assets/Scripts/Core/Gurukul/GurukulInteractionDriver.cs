using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Works out what she could interact with each frame and acts on the press when it comes.
    //
    // Runs after the player controller so the reach check uses where she actually ended up this
    // frame, not where she was before moving.
    [DefaultExecutionOrder(50)]
    public sealed class GurukulInteractionDriver : MonoBehaviour
    {
        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private GurukulActor player;

        private readonly List<GurukulInteractionCandidate> candidates = new();
        private readonly PromptHysteresis prompt = new();
        private readonly Dictionary<string, GurukulInteractionCandidate> byId = new();

        // The target the prompt should be showing, or null. Fires only when it actually changes.
        public event Action<GurukulInteractionCandidate?> PromptChanged;

        // Raised once per accepted press. Phase 4 hangs conversations off this.
        public event Action<GurukulInteractionCandidate> Interacted;

        private string shownTargetId;

        public void Bind(GurukulInputRouter inputRouter, GurukulActor protagonist)
        {
            router = inputRouter;
            player = protagonist;
        }

        private void Update()
        {
            if (router == null || player == null) return;

            if (!router.States.AcceptsWorldInteraction)
            {
                ClearPrompt();
                return;
            }

            GurukulInteractionCandidate? target = ResolveTarget();
            PublishPrompt(target);

            if (router.InteractPressed && target.HasValue) Interacted?.Invoke(target.Value);
        }

        private GurukulInteractionCandidate? ResolveTarget()
        {
            GatherCandidates();

            string resolvedId = GurukulInteractionResolver.TryResolve(
                player.Position, player.Facing, candidates,
                GurukulLocationService.Instance?.Collision, out GurukulInteractionCandidate chosen)
                ? chosen.Id
                : null;

            // Held briefly past the moment it stops being valid, so standing on the edge of a
            // target's reach doesn't strobe the prompt.
            string settledId = prompt.Tick(resolvedId, Time.deltaTime);
            return settledId != null && byId.TryGetValue(settledId, out GurukulInteractionCandidate settled)
                ? settled
                : null;
        }

        private void GatherCandidates()
        {
            candidates.Clear();
            byId.Clear();

            foreach (GurukulActor actor in GurukulWorld.Actors)
            {
                if (actor == null || actor == player || string.IsNullOrEmpty(actor.ConversationId)) continue;
                Add(new GurukulInteractionCandidate(actor.CharacterId, GurukulTargetKind.Character,
                    actor.Position, GurukulVerb.Talk));
            }

            foreach (GurukulInteractable interactable in GurukulWorld.Interactables)
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
            GurukulLocationData here = GurukulLocationService.Instance?.CurrentLocation;
            if (here == null) return;

            foreach (GurukulDoor door in here.Doors)
                Add(new GurukulInteractionCandidate(door.doorId, GurukulTargetKind.Door, door.position, door.verb));
        }

        private void Add(GurukulInteractionCandidate candidate)
        {
            candidates.Add(candidate);
            byId[candidate.Id] = candidate;
        }

        private void PublishPrompt(GurukulInteractionCandidate? target)
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
