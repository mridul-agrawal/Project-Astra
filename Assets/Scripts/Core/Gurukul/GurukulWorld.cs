using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Who and what is currently standing in the loaded room. Actors and interactables put
    // themselves on here as they appear, so the interaction check reads two small lists each frame
    // instead of sweeping the scene.
    public static class GurukulWorld
    {
        private static readonly List<GurukulActor> actors = new();
        private static readonly List<GurukulInteractable> interactables = new();

        // "Enter Play Mode" with domain reload off keeps statics between sessions, which would
        // otherwise leave the last run's destroyed actors on the list.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            actors.Clear();
            interactables.Clear();
        }

        public static IReadOnlyList<GurukulActor> Actors => actors;
        public static IReadOnlyList<GurukulInteractable> Interactables => interactables;

        public static void Register(GurukulActor actor)
        {
            if (actor != null && !actors.Contains(actor)) actors.Add(actor);
        }

        public static void Unregister(GurukulActor actor) => actors.Remove(actor);

        public static void Register(GurukulInteractable interactable)
        {
            if (interactable != null && !interactables.Contains(interactable)) interactables.Add(interactable);
        }

        public static void Unregister(GurukulInteractable interactable) => interactables.Remove(interactable);

        public static GurukulActor FindActor(string characterId)
        {
            foreach (GurukulActor actor in actors)
                if (actor != null && actor.CharacterId == characterId) return actor;
            return null;
        }

        public static GurukulInteractable FindInteractable(string interactableId)
        {
            foreach (GurukulInteractable interactable in interactables)
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
