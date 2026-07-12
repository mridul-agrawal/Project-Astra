using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One tutorial hook: when Event happens (optionally only on a given player-phase
    // turn), play Script. FireOnce triggers retire after their first play so a line
    // doesn't repeat every turn.
    [Serializable]
    internal class DialogueTrigger
    {
        [SerializeField] private BattleDialogueEventType @event;

        [Tooltip("Player-phase turn this fires on. 0 = any turn. Ignored by non-phase events.")]
        [SerializeField] private int turnFilter = 0;

        [SerializeField] private DialogueScript script;
        [SerializeField] private bool fireOnce = true;

        private bool spent;

        public DialogueScript Script => script;

        public bool Matches(BattleDialogueEventType eventType, int turn)
        {
            if (spent || script == null) return false;
            if (@event != eventType) return false;
            if (IsTurnFiltered(eventType) && turnFilter != turn) return false;
            return true;
        }

        public void MarkFired()
        {
            if (fireOnce) spent = true;
        }

        private bool IsTurnFiltered(BattleDialogueEventType eventType)
            => eventType == BattleDialogueEventType.PlayerPhaseStarted && turnFilter > 0;

        internal static DialogueTrigger CreateForTest(BattleDialogueEventType eventType,
            DialogueScript script, bool fireOnce = true, int turnFilter = 0)
        {
            return new DialogueTrigger
            {
                @event = eventType,
                script = script,
                fireOnce = fireOnce,
                turnFilter = turnFilter
            };
        }
    }

    // Picks the first matching, un-spent trigger for an event and retires it.
    // Plain class so the matching rules unit-test without a scene.
    internal class DialogueTriggerSet
    {
        private readonly IReadOnlyList<DialogueTrigger> triggers;

        public DialogueTriggerSet(IReadOnlyList<DialogueTrigger> triggers) => this.triggers = triggers;

        public DialogueScript Resolve(BattleDialogueEventType eventType, int turn)
        {
            foreach (var trigger in triggers)
            {
                if (!trigger.Matches(eventType, turn)) continue;
                trigger.MarkFired();
                return trigger.Script;
            }
            return null;
        }
    }
}
