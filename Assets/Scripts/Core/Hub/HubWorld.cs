using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Hub
{
    // Who and what is currently standing in the loaded room, by id — so an objective marker can
    // be hung on "the blackboard" without anything having to know where the blackboard is.
    public static class HubWorld
    {
        private static readonly List<HubActor> actors = new();
        private static readonly List<InspectableInteractable> inspectables = new();

        // "Enter Play Mode" with domain reload off keeps statics between sessions, which would
        // otherwise leave the last run's destroyed actors on the list.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            actors.Clear();
            inspectables.Clear();
        }

        public static IReadOnlyList<HubActor> Actors => actors;

        public static void Register(HubActor actor)
        {
            if (actor != null && !actors.Contains(actor)) actors.Add(actor);
        }

        public static void Unregister(HubActor actor) => actors.Remove(actor);

        public static void Register(InspectableInteractable inspectable)
        {
            if (inspectable != null && !inspectables.Contains(inspectable)) inspectables.Add(inspectable);
        }

        public static void Unregister(InspectableInteractable inspectable) => inspectables.Remove(inspectable);

        public static HubActor FindActor(string characterId)
        {
            foreach (HubActor actor in actors)
                if (actor != null && actor.CharacterId == characterId) return actor;
            return null;
        }

        public static InspectableInteractable FindInspectable(string interactableId)
        {
            foreach (InspectableInteractable inspectable in inspectables)
                if (inspectable != null && inspectable.InteractableId == interactableId) return inspectable;
            return null;
        }

        // Called when a room is torn down, so nothing from the old one lingers.
        public static void Clear()
        {
            actors.Clear();
            inspectables.Clear();
        }
    }
}
