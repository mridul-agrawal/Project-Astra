using UnityEngine;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Hub
{
    // Reports what just happened in the hub to the visit's objectives.
    //
    // Everything arrives as an announcement — a conversation ended, an object was looked at, a flag
    // went up — and only the kind an objective actually asked for will match. Nothing that raises
    // one of these ever finds out whether it completed anything.
    public sealed class HubProgressionListener : MonoBehaviour
    {
        [SerializeField] private HubEventRunner events;

        private void Awake()
        {
            if (events == null) events = FindFirstObjectByType<HubEventRunner>();
        }

        private void OnEnable()
        {
            InteractionEvents.ConversationFinished += OnConversationFinished;
            InteractionEvents.ObjectInspected += OnObjectInspected;
            InteractionEvents.FlagRaised += OnFlagRaised;
            if (events != null) events.FlagRaised += OnFlagRaised;
        }

        private void OnDisable()
        {
            InteractionEvents.ConversationFinished -= OnConversationFinished;
            InteractionEvents.ObjectInspected -= OnObjectInspected;
            InteractionEvents.FlagRaised -= OnFlagRaised;
            if (events != null) events.FlagRaised -= OnFlagRaised;
        }

        private static ObjectiveSequenceRunner Objectives => HubProgressService.Instance?.Objectives;

        // Reported only once the words have actually finished, which is what makes the spec's
        // completion order hold: the exchange ends, then the objective completes, then its effects
        // land, then the next objective appears.
        private static void OnConversationFinished(string conversationId) =>
            Objectives?.Report(HubConditionKind.ConversationCompleted, conversationId);

        // An objective can be written against the object she looked at rather than the words she
        // read, so an inspection is credited both ways.
        private static void OnObjectInspected(string interactableId) =>
            Objectives?.Report(HubConditionKind.ObjectInspected, interactableId);

        // A flag stands for either of the two things a conversation can conclude.
        private static void OnFlagRaised(string flagId)
        {
            ObjectiveSequenceRunner objectives = Objectives;
            if (objectives == null) return;

            objectives.Report(HubConditionKind.QuizPassed, flagId);
            objectives.Report(HubConditionKind.EventCompleted, flagId);
        }
    }
}
