using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.Dialogue
{
    // Lives on the battle map. Listens for battle moments (turn start from the turn
    // channel, selection/move/combat from the battle channel) and plays the matching
    // tutorial dialogue through DialogueService. Pure glue: matching rules live in
    // DialogueTriggerSet, display lives in DialogueService.
    public class DialogueTriggerDriver : MonoBehaviour
    {
        [SerializeField] private TurnEventChannel _turnChannel;
        [SerializeField] private BattleDialogueEventChannel _battleChannel;
        [SerializeField] private List<DialogueTrigger> _triggers = new();

        private DialogueTriggerSet _set;

        private void Awake() => _set = new DialogueTriggerSet(_triggers);

        private void OnEnable()
        {
            if (_turnChannel != null) _turnChannel.RegisterPhaseStarted(OnPhaseStarted);
            if (_battleChannel != null) _battleChannel.Register(OnBattleEvent);
        }

        private void OnDisable()
        {
            if (_turnChannel != null) _turnChannel.UnregisterPhaseStarted(OnPhaseStarted);
            if (_battleChannel != null) _battleChannel.Unregister(OnBattleEvent);
        }

        private void OnPhaseStarted(BattlePhase phase, int turn)
        {
            if (phase == BattlePhase.PlayerPhase)
                Fire(BattleDialogueEventType.PlayerPhaseStarted, turn);
        }

        private void OnBattleEvent(BattleDialogueEventType eventType)
            => Fire(eventType, CurrentTurn());

        private void Fire(BattleDialogueEventType eventType, int turn)
        {
            var script = _set.Resolve(eventType, turn);
            if (script != null) DialogueService.Instance?.Play(script, DialogueContext.BattleMap);
        }

        private static int CurrentTurn()
            => TurnManager.Instance != null ? TurnManager.Instance.TurnCounter : 0;
    }
}
