using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // A way between two rooms.
    public sealed class DoorInteractable : ConversationInteractable
    {
        [SerializeField] private string doorId;
        [SerializeField] private HubVerb verb = HubVerb.Enter;
        [SerializeField] private string requiredGate;
        [SerializeField] private string deniedConversationId;

        [SerializeField] private HubLocationTransition transitions;

        // What the loader read out of the authored room, kept so the transition can be asked for.
        private HubDoor authored;
        private bool hasAuthored;

        public string DoorId => doorId;

        public override HubVerb Verb => verb;
        public override InteractionPriority Priority => InteractionPriority.Door;

        // A door is worth walking up to whether or not it opens.
        public override bool IsAvailable => hasAuthored;

        protected override string ActiveConversationId => IsShut ? deniedConversationId : null;

        private bool IsShut =>
            !string.IsNullOrEmpty(requiredGate) && Flags != null && !Flags.IsGateOpen(requiredGate);

        // Facing only. She is standing in the doorway, and the wall the door is set into would
        // fail a line-of-sight check every time.
        public override bool CanReach(InteractorPose player) =>
            InteractionReachRules.IsFacing(player, InteractionPoint);

        public override void Interact(InteractorPose player)
        {
            if (IsShut) { Play(deniedConversationId); return; }
            if (!hasAuthored || Transitions == null) return;

            Transitions.TryUse(authored);
        }

        private HubLocationTransition Transitions =>
            transitions != null ? transitions : transitions = FindFirstObjectByType<HubLocationTransition>();

        // Stood up by the loader from the room's authored door list.
        public void Configure(HubDoor door)
        {
            authored = door;
            hasAuthored = true;

            doorId = door.doorId;
            verb = door.verb;
            requiredGate = door.requiredGate;
            deniedConversationId = door.deniedConversationId;
        }
    }
}
