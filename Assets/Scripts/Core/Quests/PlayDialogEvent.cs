using System;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Quests
{
    // Says something. The stage's own line, not a character's.
    [Serializable]
    public sealed class PlayDialogEvent : QuestEvent
    {
        [HubPick(HubIdKind.Conversation)]
        [SerializeField] private string dialogueId;

        public string DialogueId => dialogueId;

        public override void Run(IQuestWorld world) => world.PlayDialogue(dialogueId);

        public void Configure(string id) => dialogueId = id;
    }
}
