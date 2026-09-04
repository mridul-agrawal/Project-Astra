using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Something in the world she can look at: a shelf, a report card, a noticeboard.
    public sealed class InspectableInteractable : ConversationInteractable
    {
        [SerializeField] private string interactableId;

        [Tooltip("Off for an objective-critical object, which outranks a door.")]
        [SerializeField] private bool objectiveCritical;

        [SerializeField] private HubVerb verb = HubVerb.Inspect;

        [Tooltip("Where she has to stand next to, relative to this transform.")]
        [SerializeField] private Vector2 interactionOffset;

        [HubPick(HubIdKind.Conversation)]
        [SerializeField] private string conversationId;

        [Tooltip("Blocks the normal result until this gate opens. Empty for something always usable.")]
        [HubPick(HubIdKind.Gate)]
        [SerializeField] private string requiredGate;

        [Tooltip("Played instead while the gate is shut. A gated thing she can walk up to must say why.")]
        [HubPick(HubIdKind.Conversation)]
        [SerializeField] private string deniedConversationId;

        [Tooltip("State to fall back on when the visit doesn't mention this one.")]
        [SerializeField] private HubInteractableState defaultState = HubInteractableState.Available;

        public string InteractableId => interactableId;
        public string RequiredGate => requiredGate;
        public string ConversationId => conversationId;
        public string DeniedConversationId => deniedConversationId;

        // Listed by id as well as by trigger, so an objective marker can find this without knowing
        // where in the room it stands.
        private void OnEnable() => HubWorld.Register(this);

        protected override void OnDisable()
        {
            base.OnDisable();
            HubWorld.Unregister(this);
        }

        public override HubVerb Verb => verb;

        public override InteractionPriority Priority =>
            objectiveCritical ? InteractionPriority.ObjectiveObject : InteractionPriority.Inspectable;

        public override Vector2 InteractionPoint => (Vector2)transform.position + interactionOffset;

        // Inactive things aren't there as far as the player is concerned. Everything else is worth
        // walking up to, including a gated one that owes her an answer.
        public override bool IsAvailable =>
            State != HubInteractableState.Inactive && !string.IsNullOrEmpty(ActiveConversationId);

        protected override string ActiveConversationId => IsLocked ? deniedConversationId : conversationId;

        // Only the real thing counts as having inspected it; a denial line does not.
        protected override string InspectedId => IsLocked ? null : interactableId;

        private HubInteractableState State =>
            HubVisitService.Instance != null
                ? HubVisitService.Instance.Flags.GetInteractableState(interactableId, defaultState)
                : defaultState;

        private bool IsLocked =>
            State == HubInteractableState.Gated ||
            (!string.IsNullOrEmpty(requiredGate) && Flags != null && !Flags.IsGateOpen(requiredGate));

        // Everything a spawner needs to stand one of these up from authored data.
        public void Configure(string id, HubVerb showAs, Vector2 offset, string conversation,
            string gate, string denied, bool critical, HubInteractableState fallback)
        {
            interactableId = id;
            verb = showAs;
            interactionOffset = offset;
            conversationId = conversation;
            requiredGate = gate;
            deniedConversationId = denied;
            objectiveCritical = critical;
            defaultState = fallback;
        }
    }
}
