using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Hub
{
    // What a quest event reaches when it runs in the hub. The only place the quest system and the
    // hub meet, so neither has to know the other's shape.
    public sealed class HubQuestWorld : MonoBehaviour, IQuestWorld
    {
        [SerializeField] private HubEventRunner events;
        [SerializeField] private DialogueScriptCatalog scriptCatalog;

        private void Awake()
        {
            if (events == null) events = FindFirstObjectByType<HubEventRunner>();
        }

        // Written to the visit's flags. Whether a door opens on it is the door's business.
        public void SetFlag(string flagId, bool open) =>
            HubProgressService.Instance?.State.SetGate(flagId, open);

        public void PlayDialogue(string dialogueId)
        {
            DialogueScript script = Catalog != null ? Catalog.Get(dialogueId) : null;
            if (script == null)
            {
                Debug.LogError($"[HubQuestWorld] No dialogue script '{dialogueId}'.", this);
                return;
            }

            DialogueService.Instance?.Play(script, DialogueTriggeringContext.Conversation,
                onComplete: null, HubProgressService.Instance?.State, InteractionEvents.RaiseFlag);
        }

        public void PlayAuthoredEvent(string eventId)
        {
            if (events == null) return;
            events.TryPlay(eventId);
        }

        private DialogueScriptCatalog Catalog =>
            scriptCatalog != null ? scriptCatalog : scriptCatalog = HubInteractionCatalog.Scripts;
    }
}
