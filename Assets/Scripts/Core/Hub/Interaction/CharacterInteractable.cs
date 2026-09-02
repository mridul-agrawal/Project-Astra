using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Someone she can talk to.
    [RequireComponent(typeof(HubActor))]
    public sealed class CharacterInteractable : ConversationInteractable
    {
        private HubActor actor;

        public override HubVerb Verb => HubVerb.Talk;
        public override InteractionPriority Priority => InteractionPriority.Character;
        public override Vector2 InteractionPoint => Actor != null ? Actor.Position : (Vector2)transform.position;

        // Nothing to say means no prompt and no blank dialogue box.
        public override bool IsAvailable => !string.IsNullOrEmpty(ActiveConversationId);

        protected override string ActiveConversationId => Actor != null ? Actor.ConversationId : null;

        private HubActor Actor => actor != null ? actor : actor = GetComponent<HubActor>();

        // They look at each other before the portraits come up. Her facing is left alone — she is
        // already facing them, or she could not have started this.
        public override void Interact(InteractorPose player)
        {
            if (Actor != null) Actor.SetFacing(Cardinal.Opposite(player.Facing));
            base.Interact(player);
        }
    }
}
