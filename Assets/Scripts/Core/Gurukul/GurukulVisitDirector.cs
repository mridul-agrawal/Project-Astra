using UnityEngine;
using ProjectAstra.Core.Dialogue.Conversation;
using ProjectAstra.Core.Gurukul.Events;

namespace ProjectAstra.Core.Gurukul
{
    // Joins the three halves of a visit: she presses INTERACT, a conversation runs, and the
    // objective it belonged to ticks over.
    //
    // Progress is only ever reported once a conversation has actually finished, which is what makes
    // the spec's completion order hold — the exchange ends, then the objective completes, then its
    // effects land, then the next objective appears.
    public sealed class GurukulVisitDirector : MonoBehaviour
    {
        [SerializeField] private GurukulInteractionDriver interactionDriver;
        [SerializeField] private ConversationPlayer conversations;
        [SerializeField] private GurukulLocationTransition transitions;
        [SerializeField] private GurukulEventRunner events;
        [SerializeField] private GurukulDepartureController departures;

        // What she was standing in front of when the running conversation started, so an inspection
        // credits the object as well as the conversation.
        private GurukulInteractionCandidate? openedFrom;

        private void Awake()
        {
            if (interactionDriver == null) interactionDriver = FindFirstObjectByType<GurukulInteractionDriver>();
            if (conversations == null) conversations = FindFirstObjectByType<ConversationPlayer>();
            if (transitions == null) transitions = FindFirstObjectByType<GurukulLocationTransition>();
            if (events == null) events = FindFirstObjectByType<GurukulEventRunner>();
            if (departures == null) departures = FindFirstObjectByType<GurukulDepartureController>();
        }

        private void OnEnable()
        {
            if (interactionDriver != null) interactionDriver.Interacted += OnInteracted;
            if (conversations != null)
            {
                conversations.ConversationFinished += OnConversationFinished;
                conversations.FlagRaised += OnFlagRaised;
            }
            if (events == null) return;
            events.FlagRaised += OnFlagRaised;
            events.DepartureRequested += OnDepartureRequested;
        }

        private void OnDisable()
        {
            if (interactionDriver != null) interactionDriver.Interacted -= OnInteracted;
            if (conversations != null)
            {
                conversations.ConversationFinished -= OnConversationFinished;
                conversations.FlagRaised -= OnFlagRaised;
            }
            if (events == null) return;
            events.FlagRaised -= OnFlagRaised;
            events.DepartureRequested -= OnDepartureRequested;
        }

        // The visit's memory of what has already been said. Handed over once the visit is loaded,
        // because the conversation player itself knows nothing about hubs or visits.
        public void BindVisitMemory(IConversationMemory memory)
        {
            if (conversations != null) conversations.Memory = memory;
        }

        private void OnDepartureRequested(string _) => departures?.TryDepart();

        // An objective's effect can ask for an event by name; this is the only path between the two,
        // so the objective system never needs to know what an event is.
        public void PlayEvent(string eventId)
        {
            if (events != null) events.TryPlay(eventId);
        }

        private void OnInteracted(GurukulInteractionCandidate target)
        {
            if (target.Kind == GurukulTargetKind.Door)
            {
                UseDoor(target.Id);
                return;
            }

            StartConversation(ConversationFor(target), target);
        }

        // A shut door still answers — it plays its denial line instead of going anywhere, which is
        // the spec's rule that a gate is never just an invisible wall.
        private void UseDoor(string doorId)
        {
            GurukulLocationData here = GurukulLocationService.Instance?.CurrentLocation;
            if (here == null || !here.TryGetDoor(doorId, out GurukulDoor door)) return;

            if (IsShut(door))
            {
                StartConversation(door.deniedConversationId, null);
                return;
            }

            transitions.TryUse(door);
        }

        private static bool IsShut(GurukulDoor door)
        {
            if (string.IsNullOrEmpty(door.requiredGate)) return false;
            GurukulRuntimeState state = GurukulProgressService.Instance?.State;
            return state != null && !state.IsGateOpen(door.requiredGate);
        }

        private void StartConversation(string conversationId, GurukulInteractionCandidate? from)
        {
            if (string.IsNullOrEmpty(conversationId)) return;
            openedFrom = conversations.TryStart(conversationId) ? from : null;
        }

        private static string ConversationFor(GurukulInteractionCandidate target)
        {
            if (target.Kind == GurukulTargetKind.Character)
                return GurukulWorld.FindActor(target.Id)?.ConversationId;

            return GurukulWorld.FindInteractable(target.Id)?.ActiveConversationId;
        }

        private void OnConversationFinished(string conversationId)
        {
            ObjectiveSequenceRunner objectives = GurukulProgressService.Instance?.Objectives;
            if (objectives == null) return;

            objectives.Report(GurukulConditionKind.ConversationCompleted, conversationId);

            // An objective can be written against the object she looked at rather than the words she
            // read, so an inspection credits both. Only the kind the objective actually asked for
            // will match.
            if (openedFrom is { Kind: not GurukulTargetKind.Character })
                objectives.Report(GurukulConditionKind.ObjectInspected, openedFrom.Value.Id);

            openedFrom = null;
        }

        // A flag can stand for either of the two things a conversation can conclude, and only the
        // one an objective actually named will be credited.
        private void OnFlagRaised(string flagId)
        {
            // A confirmation conversation leaves through its own flag, so both departure modes end
            // at the same checks.
            if (flagId == GurukulDepartureController.DepartFlag)
            {
                departures?.TryDepart();
                return;
            }

            ObjectiveSequenceRunner objectives = GurukulProgressService.Instance?.Objectives;
            if (objectives == null) return;

            objectives.Report(GurukulConditionKind.QuizPassed, flagId);
            objectives.Report(GurukulConditionKind.EventCompleted, flagId);
        }
    }
}
