using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Something in the world she can press INTERACT on: a shelf, a door, a report card on a table.
    //
    // Its availability lives in the visit's runtime state rather than on the component, so an
    // objective can switch it on, gate it or use it up without anything having to find and poke the
    // GameObject.
    public sealed class HubInteractable : MonoBehaviour
    {
        [SerializeField] private string interactableId;

        [Tooltip("Decides which rung of the interaction priority ladder this sits on.")]
        [SerializeField] private HubTargetKind kind = HubTargetKind.Inspectable;

        [SerializeField] private HubVerb verb = HubVerb.Inspect;

        [Tooltip("Where she has to stand next to, relative to this transform. A wide counter puts this at the side you approach from.")]
        [SerializeField] private Vector2 footOffset;

        [SerializeField] private string conversationId;

        [Tooltip("Blocks the normal result until this gate opens. Leave empty for something always usable.")]
        [SerializeField] private string requiredGate;

        [Tooltip("Played instead while the gate is shut. A gated thing she can still walk up to must say why, not fail silently.")]
        [SerializeField] private string deniedConversationId;

        [Tooltip("State to fall back on when the visit doesn't mention this one.")]
        [SerializeField] private HubInteractableState defaultState = HubInteractableState.Available;

        private void OnEnable() => HubWorld.Register(this);
        private void OnDisable() => HubWorld.Unregister(this);

        public string InteractableId => interactableId;
        public HubTargetKind Kind => kind;
        public HubVerb Verb => verb;
        public Vector2 FootPosition => (Vector2)transform.position + footOffset;

        public HubInteractableState State =>
            HubProgressService.Instance != null
                ? HubProgressService.Instance.State.GetInteractableState(interactableId, defaultState)
                : defaultState;

        // Inactive things aren't there as far as the player is concerned — no prompt, no dialogue.
        // Everything else is worth walking up to, including a gated door that owes her an answer.
        public bool IsPresent => State != HubInteractableState.Inactive;

        public bool IsGated =>
            State == HubInteractableState.Gated ||
            (!string.IsNullOrEmpty(requiredGate) &&
             HubProgressService.Instance != null &&
             !HubProgressService.Instance.State.IsGateOpen(requiredGate));

        // What talking to or inspecting this actually plays right now.
        public string ActiveConversationId => IsGated ? deniedConversationId : conversationId;

        public HubInteractionCandidate ToCandidate() => new(interactableId, kind, FootPosition, verb);
    }
}
