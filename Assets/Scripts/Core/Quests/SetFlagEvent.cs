using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // Raises or lowers a named flag. What the world does about it is the world's business.
    [Serializable]
    public sealed class SetFlagEvent : QuestEvent
    {
        [SerializeField] private string flagId;
        [SerializeField] private bool open = true;

        public override void Run(IQuestWorld world) => world.SetFlag(flagId, open);

        public void Configure(string id, bool isOpen = true)
        {
            flagId = id;
            open = isOpen;
        }
    }
}
