using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Holds the one thing a press would act on, and acts on it.
    public sealed class PlayerInteractionController
    {
        private readonly List<IInteractable> inRange = new();
        private readonly InteractLatch confirm = new();

        // Fires when the thing worth prompting for changes, including to nothing.
        public event Action<IInteractable> TargetChanged;

        public IInteractable CurrentTarget { get; private set; }
        public IReadOnlyList<IInteractable> TargetsInRange => inRange;

        // Whether the hub is in control. False silences everything, which is how the spec's first
        // priority rung — an open menu or a running line — is honoured without being a rung.
        public bool Enabled { get; set; } = true;

        // Called by whatever detects the player arriving; today the interactable's own trigger.
        public void Add(IInteractable interactable)
        {
            if (interactable == null || inRange.Contains(interactable)) return;
            inRange.Add(interactable);
        }

        public void Remove(IInteractable interactable)
        {
            if (interactable == null) return;
            inRange.Remove(interactable);
            if (ReferenceEquals(CurrentTarget, interactable)) SetTarget(null);
        }

        // A press held through a door or a closing dialogue must not count afterwards.
        public void SuppressCurrentPress() => confirm.Suppress();

        public void Tick(InteractorPose player, bool confirmHeld)
        {
            bool pressed = confirm.Consume(confirmHeld);

            if (!Enabled)
            {
                SetTarget(null);
                return;
            }

            SetTarget(ResolvePriority(player));

            // Re-checked rather than trusted: the target may have become unavailable between the
            // prompt appearing and the button going down.
            if (!pressed || CurrentTarget == null) return;
            if (!IsActionable(CurrentTarget, player)) return;

            CurrentTarget.Interact(player);
        }

        // The one target a press would act on: the best rung, then most directly in front, then
        // nearest. Null when nothing qualifies.
        public IInteractable ResolvePriority(InteractorPose player)
        {
            IInteractable best = null;
            float bestLateral = 0f, bestDistance = 0f;

            foreach (IInteractable candidate in inRange)
            {
                if (!IsActionable(candidate, player)) continue;

                float lateral = InteractionReachRules.LateralOffset(player, candidate.InteractionPoint);
                float distance = Vector2.Distance(player.Position, candidate.InteractionPoint);

                if (best != null && !Beats(candidate, lateral, distance, best, bestLateral, bestDistance)) continue;

                best = candidate;
                bestLateral = lateral;
                bestDistance = distance;
            }
            return best;
        }

        private static bool IsActionable(IInteractable candidate, InteractorPose player) =>
            candidate != null && candidate.IsAvailable && candidate.CanReach(player);

        private static bool Beats(IInteractable candidate, float lateral, float distance,
            IInteractable best, float bestLateral, float bestDistance)
        {
            if (candidate.Priority != best.Priority) return candidate.Priority < best.Priority;
            if (!Mathf.Approximately(lateral, bestLateral)) return lateral < bestLateral;
            return distance < bestDistance;
        }

        private void SetTarget(IInteractable target)
        {
            if (ReferenceEquals(target, CurrentTarget)) return;

            CurrentTarget = target;
            TargetChanged?.Invoke(target);
        }
    }
}
