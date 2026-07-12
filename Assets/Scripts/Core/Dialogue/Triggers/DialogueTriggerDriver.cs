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
        [SerializeField] private List<DialogueTrigger> triggers = new();

        private DialogueTriggerSet set;

        private void Awake() => set = new DialogueTriggerSet(triggers);

        private void OnEnable()
        {
            EventService.Instance.SubscribePhaseBannerFinished(OnPhaseBannerFinished);
            EventService.Instance.SubscribeBattleDialogue(OnBattleEvent);
        }

        private void OnDisable()
        {
            if (EventService.Instance != null)
            {
                EventService.Instance.UnsubscribePhaseBannerFinished(OnPhaseBannerFinished);
                EventService.Instance.UnsubscribeBattleDialogue(OnBattleEvent);
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
            var script = set.Resolve(eventType, turn);
            if (script != null) DialogueService.Instance?.Play(script, DialogueTriggeringContext.BattleMap);
        }

        private static int CurrentTurn()
            => TurnManager.Instance != null ? TurnManager.Instance.TurnCounter : 0;
    }
}
