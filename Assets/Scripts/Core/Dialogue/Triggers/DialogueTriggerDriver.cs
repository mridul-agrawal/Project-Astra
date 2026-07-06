using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.Dialogue
{
    // Lives on the battle map. Listens for battle moments (the phase banner finishing,
    // from the turn channel; selection/move/combat from the battle channel) and plays the
    // matching tutorial dialogue through DialogueService. Pure glue: matching rules live in
    // DialogueTriggerSet, display lives in DialogueService.
    public class DialogueTriggerDriver : MonoBehaviour
    {
        [SerializeField] private List<DialogueTrigger> _triggers = new();

        private DialogueTriggerSet _set;

        private void Awake() => _set = new DialogueTriggerSet(_triggers);

        private void OnEnable()
        {
            EventService.Instance.Turn.RegisterPhaseBannerFinished(OnPhaseBannerFinished);
            EventService.Instance.BattleDialogue.Register(OnBattleEvent);
        }

        private void OnDisable()
        {
            if (EventService.Instance != null)
            {
                EventService.Instance.Turn.UnregisterPhaseBannerFinished(OnPhaseBannerFinished);
                EventService.Instance.BattleDialogue.Unregister(OnBattleEvent);
            }
        }

        // Fire the player-phase dialogue only after the phase banner has finished, so the
        // banner and the dialogue don't appear on top of each other.
        private void OnPhaseBannerFinished(BattlePhase phase, int turn)
        {
            if (phase == BattlePhase.PlayerPhase)
                Fire(BattleDialogueEventType.PlayerPhaseStarted, turn);
        }

        private void OnBattleEvent(BattleDialogueEventType eventType)
            => Fire(eventType, CurrentTurn());

        private void Fire(BattleDialogueEventType eventType, int turn)
        {
            var script = _set.Resolve(eventType, turn);
            if (script != null) DialogueService.Instance?.Play(script, DialogueTriggeringContext.BattleMap);
        }

        private static int CurrentTurn()
            => TurnManager.Instance != null ? TurnManager.Instance.TurnCounter : 0;
    }
}
