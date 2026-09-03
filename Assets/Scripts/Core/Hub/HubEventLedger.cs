using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Which scripted sequences have already run, so a one-time event never runs twice.
    [Serializable]
    public class HubEventLedger
    {
        [SerializeField] private List<string> completedEventIds = new();

        public bool HasCompletedEvent(string eventId) => completedEventIds.Contains(eventId);

        public void MarkEventCompleted(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId) && !completedEventIds.Contains(eventId))
                completedEventIds.Add(eventId);
        }
    }
}
