using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Controller for the Objective panel. The objective line is static config; the
    // turn counter tracks TurnManager via the turn/phase event channel. The panel
    // stays visible in every phase.
    public sealed class ObjectiveController
    {
        public ObjectiveView objectiveView; // wired by BattleMapHUDBuilder

        [TextArea] public string ObjectiveText = "Slay the Asura Lord";

        private readonly ObjectiveModel model = new ObjectiveModel();

        public ObjectiveController(ObjectiveView objectiveView)
        {
            this.objectiveView = objectiveView;

            //EventService.Instance.SubscribePhaseStarted(HandlePhaseStarted);
            //EventService.Instance.SubscribeTurnAdvanced(HandleTurnAdvanced);
        }

        private void Start()
        {
            var tm = TurnManager.Instance;
            model.ObjectiveText = ObjectiveText;
            model.Turn = tm != null ? tm.TurnCounter : 1;
            Render();
        }

        private void OnDestroy()
        {
            if (EventService.Instance == null) return;
            EventService.Instance.UnsubscribePhaseStarted(HandlePhaseStarted);
            EventService.Instance.UnsubscribeTurnAdvanced(HandleTurnAdvanced);
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber) => SetTurn(turnNumber);
        private void HandleTurnAdvanced(int turnNumber) => SetTurn(turnNumber);

        private void SetTurn(int turnNumber)
        {
            model.Turn = turnNumber;
            Render();
        }

        private void Render()
        {
            if (objectiveView != null) objectiveView.Render(model);
        }
    }
}
