using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Shared by everything whose whole answer to a press is "say something".
    public abstract class ConversationInteractable : InteractableBehaviour
    {
        [SerializeField] private DialogueScriptCatalog scriptCatalog;

        // The conversation to play right now, or empty for nothing to say.
        protected abstract string ActiveConversationId { get; }

        // Announced after the conversation ends, so an objective written against the object rather
        // than the words still gets its credit. Empty for something nobody tracks.
        protected virtual string InspectedId => null;

        public override void Interact(InteractorPose player) => Play(ActiveConversationId);

        protected void Play(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId)) return;
            if (DialogueService.Instance == null || DialogueService.Instance.IsPlaying) return;

            DialogueScript script = Catalog != null ? Catalog.Get(conversationId) : null;
            if (script == null)
            {
                Debug.LogError($"[{GetType().Name}] No dialogue script with id '{conversationId}'. " +
                               "Add it to the Dialogue Script Catalog.", this);
                return;
            }

            DialogueService.Instance.Play(script, DialogueTriggeringContext.Conversation,
                () => Finished(conversationId),
                HubVisitService.Instance?.Dialogue,
                InteractionEvents.RaiseFlag);
        }

        // Said in its own words. Whether any of it completes anything is somebody else's question.
        private void Finished(string conversationId)
        {
            InteractionEvents.RaiseConversationFinished(conversationId);

            if (!string.IsNullOrEmpty(InspectedId))
                InteractionEvents.RaiseObjectInspected(InspectedId);
        }

        // Found rather than wired when a spawner built this at runtime and had no catalog to hand.
        private DialogueScriptCatalog Catalog =>
            scriptCatalog != null ? scriptCatalog : scriptCatalog = HubInteractionCatalog.Scripts;

        public void BindCatalog(DialogueScriptCatalog catalog) => scriptCatalog = catalog;
    }
}
