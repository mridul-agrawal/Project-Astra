using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // A way between two rooms.
    public sealed class DoorInteractable : ConversationInteractable
    {
        [Tooltip("Where this door leads, and what it takes to open it. Authored here, in the room.")]
        [SerializeField] private HubDoor door;

        [SerializeField] private HubLocationTransition transitions;

        public string DoorId => door.doorId;

        public override HubVerb Verb => door.verb;
        public override InteractionPriority Priority => InteractionPriority.Door;

        // A door is worth walking up to whether or not it opens.
        public override bool IsAvailable => true;

        protected override string ActiveConversationId => IsShut ? door.deniedConversationId : null;

        private bool IsShut =>
            !string.IsNullOrEmpty(door.requiredGate) && Flags != null && !Flags.IsGateOpen(door.requiredGate);

        // Facing only. She is standing in the doorway, and the wall the door is set into would
        // fail a line-of-sight check every time.
        public override bool CanReach(InteractorPose player) =>
            InteractionReachRules.IsFacing(player, InteractionPoint);

        public override void Interact(InteractorPose player)
        {
            if (IsShut) { Play(door.deniedConversationId); return; }
            if (Transitions == null) return;

            Transitions.TryUse(door);
        }

        private HubLocationTransition Transitions =>
            transitions != null ? transitions : transitions = FindFirstObjectByType<HubLocationTransition>();
    }
}
