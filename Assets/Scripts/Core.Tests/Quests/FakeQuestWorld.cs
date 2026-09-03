using System.Collections.Generic;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Tests.Quests
{
    // Records what a quest event asked the world to do, in the order it asked.
    public sealed class FakeQuestWorld : IQuestWorld
    {
        public readonly List<string> Log = new();

        public void SetFlag(string flagId, bool open) => Log.Add($"flag:{flagId}={open}");
        public void PlayDialogue(string dialogueId) => Log.Add($"dialogue:{dialogueId}");
        public void PlayAuthoredEvent(string eventId) => Log.Add($"event:{eventId}");
    }
}
