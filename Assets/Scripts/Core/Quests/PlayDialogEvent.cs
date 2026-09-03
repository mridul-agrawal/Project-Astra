using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // Says something. The stage's own line, not a character's.
    [Serializable]
    public sealed class PlayDialogEvent : QuestEvent
    {
        [SerializeField] private string dialogueId;

        public override void Run(IQuestWorld world) => world.PlayDialogue(dialogueId);

        public void Configure(string id) => dialogueId = id;
    }
}
