using ProjectAstra.Core.Hub.Interaction;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Why a thing she is standing next to is not being offered.
    //
    // The rules that decide it are spread across the interactable, its reach and the one that won,
    // so without this the answer is "read three classes and guess".
    public static class HubReachReport
    {
        public const string Offered = "this is what a press would do";
        public const string NothingToOffer = "it has nothing to offer right now";
        public const string NotFacing = "she is not facing it";
        public const string InTheWay = "something is between her and it";

        public static string Why(IInteractable candidate, InteractorPose player, IInteractable chosen)
        {
            if (candidate == null) return "there is nothing there";
            if (!candidate.IsAvailable) return NothingToOffer;

            if (!candidate.CanReach(player)) return WhyOutOfReach(candidate, player);

            return ReferenceEquals(chosen, candidate) ? Offered : Beaten(chosen);
        }

        // Reach is one answer from the interactable, so which half failed is worked out from the
        // same two rules it is built from.
        private static string WhyOutOfReach(IInteractable candidate, InteractorPose player) =>
            InteractionReachRules.IsFacing(player, candidate.InteractionPoint) ? InTheWay : NotFacing;

        private static string Beaten(IInteractable chosen) =>
            chosen == null ? "nothing is being offered" : $"{Name(chosen)} is being offered instead";

        public static string Name(IInteractable interactable)
        {
            if (interactable is Component part) return $"{interactable.Verb} {part.gameObject.name}";
            return interactable != null ? interactable.Verb.ToString() : "nothing";
        }
    }
}
