using System;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Quests
{
    // Hands a stage over to an authored scripted sequence, which owns the scene while it runs.
    [Serializable]
    public sealed class PlayAuthoredEventEvent : QuestEvent
    {
        [HubPick(HubIdKind.Event)]
        [SerializeField] private string eventId;

        public string EventId => eventId;

        public override void Run(IQuestWorld world) => world.PlayAuthoredEvent(eventId);

        public void Configure(string id) => eventId = id;
    }
}
