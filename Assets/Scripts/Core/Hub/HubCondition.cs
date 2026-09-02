using System;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Append only — authored conditions store the kind as an int.
    public enum HubConditionKind
    {
        ConversationCompleted,
        ObjectInspected,
        QuizPassed,
        EventCompleted,
        // Completes the moment it activates. For a stage whose only job is to run its effects,
        // like an opening beat that just opens a gate and hands over to the next objective.
        Immediate
    }

    // What has to happen for an objective to be done. Every target must be satisfied, so a single
    // id is "do this one thing" and several ids are "do all of these, in any order" — which is the
    // 0/5 counter. Deliberately one kind per condition: no content in the demo mixes kinds, and a
    // full expression tree would be a lot of machinery for a case that hasn't come up.
    [Serializable]
    public class HubCondition
    {
        public HubConditionKind kind;

        [Tooltip("Every id here must be satisfied. One id is a single target; several make a set the player can clear in any order.")]
        public string[] targetIds = Array.Empty<string>();

        [Tooltip("Show progress as current/total next to the objective text.")]
        public bool showCounter;

        public int RequiredCount => kind == HubConditionKind.Immediate ? 0 : TargetCount;

        private int TargetCount => targetIds != null ? targetIds.Length : 0;

        public bool Accepts(HubConditionKind reportedKind, string targetId)
        {
            if (reportedKind != kind || string.IsNullOrEmpty(targetId)) return false;
            return Array.IndexOf(targetIds, targetId) >= 0;
        }
    }
}
