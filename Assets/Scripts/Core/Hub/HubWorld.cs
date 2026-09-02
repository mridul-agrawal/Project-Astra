using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Who and what is currently standing in the loaded room. Actors and interactables put
    // themselves on here as they appear, so the interaction check reads two small lists each frame
    // instead of sweeping the scene.
    public static class HubWorld
    {
        private static readonly List<HubActor> actors = new();
        private static readonly List<HubInteractable> interactables = new();

        // "Enter Play Mode" with domain reload off keeps statics between sessions, which would
        // otherwise leave the last run's destroyed actors on the list.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            actors.Clear();
            interactables.Clear();
        }

        public static IReadOnlyList<HubActor> Actors => actors;
        public static IReadOnlyList<HubInteractable> Interactables => interactables;

        public static void Register(HubActor actor)
        {
            if (actor != null && !actors.Contains(actor)) actors.Add(actor);
        }

        public static void Unregister(HubActor actor) => actors.Remove(actor);

        public static void Register(HubInteractable interactable)
        {
            if (interactable != null && !interactables.Contains(interactable)) interactables.Add(interactable);
        }

        public static void Unregister(HubInteractable interactable) => interactables.Remove(interactable);

        public static HubActor FindActor(string characterId)
        {
            foreach (HubActor actor in actors)
                if (actor != null && actor.CharacterId == characterId) return actor;
            return null;
        }

        public static HubInteractable FindInteractable(string interactableId)
        {
            foreach (HubInteractable interactable in interactables)
                if (interactable != null && interactable.InteractableId == interactableId) return interactable;
            return null;
        }

        // Called when a room is torn down, so nothing from the old one lingers.
        public static void Clear()
        {
            actors.Clear();
            interactables.Clear();
        }
    }
}
