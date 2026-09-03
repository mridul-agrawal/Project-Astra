using System;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // Hands a stage over to an authored scripted sequence, which owns the scene while it runs.
    [Serializable]
    public sealed class PlayAuthoredEventEvent : QuestEvent
    {
        [SerializeField] private string eventId;

        public override void Run(IQuestWorld world) => world.PlayAuthoredEvent(eventId);

        public void Configure(string id) => eventId = id;
    }
}
