using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Hub
{
    // Joins the three halves of a visit: she presses INTERACT, a conversation runs, and the
    // objective it belonged to ticks over.
    //
    // Progress is only ever reported once a conversation has actually finished, which is what makes
    // the spec's completion order hold — the exchange ends, then the objective completes, then its
    // effects land, then the next objective appears.
    public sealed class HubVisitDirector : MonoBehaviour
    {
        [SerializeField] private HubInteractionDriver interactionDriver;

        [Tooltip("Turns a conversation id into the script to play.")]
        [SerializeField] private DialogueScriptCatalog scriptCatalog;
        [SerializeField] private HubLocationTransition transitions;
        [SerializeField] private HubEventRunner events;
        [SerializeField] private HubDepartureController departures;

        // What she was standing in front of when the running conversation started, so an inspection
        // credits the object as well as the conversation.
        private HubInteractionCandidate? openedFrom;

        private void Awake()
        {
            if (interactionDriver == null) interactionDriver = FindFirstObjectByType<HubInteractionDriver>();
            if (transitions == null) transitions = FindFirstObjectByType<HubLocationTransition>();
            if (events == null) events = FindFirstObjectByType<HubEventRunner>();
            if (departures == null) departures = FindFirstObjectByType<HubDepartureController>();
        }

        private void OnEnable()
        {
            if (interactionDriver != null) interactionDriver.Interacted += OnInteracted;
            if (events == null) return;
            events.FlagRaised += OnFlagRaised;
            events.DepartureRequested += OnDepartureRequested;
        }

        private void OnDisable()
        {
            if (interactionDriver != null) interactionDriver.Interacted -= OnInteracted;
            if (events == null) return;
            events.FlagRaised -= OnFlagRaised;
            events.DepartureRequested -= OnDepartureRequested;
        }

        // The visit's memory of what has already been said, handed to every script this visit
        // plays. The dialogue system never learns what a visit is.
        private IDialogueMemory memory;

        public void BindVisitMemory(IDialogueMemory visitMemory) => memory = visitMemory;

        private void OnDepartureRequested(string _) => departures?.TryDepart();

        // An objective's effect can ask for an event by name; this is the only path between the two,
        // so the objective system never needs to know what an event is.
        public void PlayEvent(string eventId)
        {
            if (events != null) events.TryPlay(eventId);
        }

        private void OnInteracted(HubInteractionCandidate target)
        {
            if (target.Kind == HubTargetKind.Door)
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
            HubLocationData here = HubLocationService.Instance?.CurrentLocation;
            if (here == null || !here.TryGetDoor(doorId, out HubDoor door)) return;

            if (IsShut(door))
            {
                StartConversation(door.deniedConversationId, null);
                return;
            }

            transitions.TryUse(door);
        }

        private static bool IsShut(HubDoor door)
        {
            if (string.IsNullOrEmpty(door.requiredGate)) return false;
            HubRuntimeState state = HubProgressService.Instance?.State;
            return state != null && !state.IsGateOpen(door.requiredGate);
        }

        // One call, the same one a cutscene makes. The service takes the Dialogue state, owns the
        // buttons for the exchange, and hands control back where it found it.
        private void StartConversation(string conversationId, HubInteractionCandidate? from)
        {
            if (string.IsNullOrEmpty(conversationId)) return;
            if (DialogueService.Instance == null || DialogueService.Instance.IsPlaying) return;

            DialogueScript script = scriptCatalog != null ? scriptCatalog.Get(conversationId) : null;
            if (script == null)
            {
                Debug.LogError($"[HubVisitDirector] No dialogue script with id '{conversationId}'. " +
                               "Add it to the Dialogue Script Catalog.");
                return;
            }

            openedFrom = from;
            DialogueService.Instance.Play(script, DialogueTriggeringContext.Conversation,
                () => OnConversationFinished(conversationId), memory, OnFlagRaised);
        }

        private static string ConversationFor(HubInteractionCandidate target)
        {
            if (target.Kind == HubTargetKind.Character)
                return HubWorld.FindActor(target.Id)?.ConversationId;

            return HubWorld.FindInteractable(target.Id)?.ActiveConversationId;
        }

        private void OnConversationFinished(string conversationId)
        {
            ObjectiveSequenceRunner objectives = HubProgressService.Instance?.Objectives;
            if (objectives == null) return;

            objectives.Report(HubConditionKind.ConversationCompleted, conversationId);

            // An objective can be written against the object she looked at rather than the words she
            // read, so an inspection credits both. Only the kind the objective actually asked for
            // will match.
            if (openedFrom is { Kind: not HubTargetKind.Character })
                objectives.Report(HubConditionKind.ObjectInspected, openedFrom.Value.Id);

            openedFrom = null;
        }

        // A flag can stand for either of the two things a conversation can conclude, and only the
        // one an objective actually named will be credited.
        private void OnFlagRaised(string flagId)
        {
            // A confirmation conversation leaves through its own flag, so both departure modes end
            // at the same checks.
            if (flagId == HubDepartureController.DepartFlag)
            {
                departures?.TryDepart();
                return;
            }

            ObjectiveSequenceRunner objectives = HubProgressService.Instance?.Objectives;
            if (objectives == null) return;

            objectives.Report(HubConditionKind.QuizPassed, flagId);
            objectives.Report(HubConditionKind.EventCompleted, flagId);
        }
    }
}
