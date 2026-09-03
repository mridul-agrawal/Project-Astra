using System;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Hub.Interaction
{
    // What an interactable announces after it has done something, in its own words.
    public static class InteractionEvents
    {
        // "Enter Play Mode" with domain reload off keeps statics between sessions, which would
        // leave the last run's listeners subscribed to objects that no longer exist.
        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ConversationFinished = null;
            ObjectInspected = null;
            FlagRaised = null;
        }

        public static event Action<string> ConversationFinished;
        public static event Action<string> ObjectInspected;
        public static event Action<string> FlagRaised;

        public static void RaiseConversationFinished(string conversationId)
        {
            ConversationFinished?.Invoke(conversationId);
            Signal(GameplaySignal.Conversation(conversationId));
        }

        public static void RaiseObjectInspected(string interactableId)
        {
            ObjectInspected?.Invoke(interactableId);
            Signal(GameplaySignal.Inspection(interactableId));
        }

        public static void RaiseFlag(string flagId)
        {
            FlagRaised?.Invoke(flagId);
            Signal(GameplaySignal.Signal(flagId));
        }

        // The same announcement, said again on the seam the quest system listens to. Both paths
        // are live while the old progression is still driving the HUD.
        private static void Signal(GameplaySignal signal) =>
            EventService.Instance?.RaiseGameplaySignal(signal);
    }
}
