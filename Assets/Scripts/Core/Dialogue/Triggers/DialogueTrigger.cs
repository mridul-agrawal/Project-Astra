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
        [SerializeField] private BattleDialogueEventType _event;

        [Tooltip("Player-phase turn this fires on. 0 = any turn. Ignored by non-phase events.")]
        [SerializeField] private int _turnFilter = 0;

        [SerializeField] private DialogueScript _script;
        [SerializeField] private bool _fireOnce = true;

        private bool _spent;

        public DialogueScript Script => _script;

        public bool Matches(BattleDialogueEventType eventType, int turn)
        {
            if (_spent || _script == null) return false;
            if (_event != eventType) return false;
            if (IsTurnFiltered(eventType) && _turnFilter != turn) return false;
            return true;
        }

        public void MarkFired()
        {
            if (_fireOnce) _spent = true;
        }

        private bool IsTurnFiltered(BattleDialogueEventType eventType)
            => eventType == BattleDialogueEventType.PlayerPhaseStarted && _turnFilter > 0;

        internal static DialogueTrigger CreateForTest(BattleDialogueEventType eventType,
            DialogueScript script, bool fireOnce = true, int turnFilter = 0)
        {
            return new DialogueTrigger
            {
                _event = eventType,
                _script = script,
                _fireOnce = fireOnce,
                _turnFilter = turnFilter
            };
        }
    }

    // Picks the first matching, un-spent trigger for an event and retires it.
    // Plain class so the matching rules unit-test without a scene.
    internal class DialogueTriggerSet
    {
        private readonly IReadOnlyList<DialogueTrigger> _triggers;

        public DialogueTriggerSet(IReadOnlyList<DialogueTrigger> triggers) => _triggers = triggers;

        public DialogueScript Resolve(BattleDialogueEventType eventType, int turn)
        {
            foreach (var trigger in _triggers)
            {
                if (!trigger.Matches(eventType, turn)) continue;
                trigger.MarkFired();
                return trigger.Script;
            }
            return null;
        }
    }
}
