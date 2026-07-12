using System;
using UnityEngine;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.Events
{
    // A pub/sub channel for turn-cycle events: a phase just started, a phase just ended, or the
    // round counter just ticked. Stored as a ScriptableObject so broadcasters (TurnManager) and
    // listeners (HUD, banners, terrain effects...) can share one asset reference.
    [CreateAssetMenu(fileName = "TurnEventChannel", menuName = "Project Astra/Core/Turn Event Channel")]
    public class TurnEventChannel : ScriptableObject
    {
        private Action<BattlePhase, int> onPhaseStarted;
        private Action<BattlePhase, int> onPhaseBannerFinished;
        private Action<BattlePhase> onPhaseEnded;
        private Action<int> onTurnAdvanced;

        public void RegisterPhaseStarted(Action<BattlePhase, int> listener) => onPhaseStarted += listener;
        public void UnregisterPhaseStarted(Action<BattlePhase, int> listener) => onPhaseStarted -= listener;

        // Raised by the phase banner once its intro animation has finished, so phase-start
        // dialogue can wait for the banner instead of appearing on top of it.
        public void RegisterPhaseBannerFinished(Action<BattlePhase, int> listener) => onPhaseBannerFinished += listener;
        public void UnregisterPhaseBannerFinished(Action<BattlePhase, int> listener) => onPhaseBannerFinished -= listener;

        public void RegisterPhaseEnded(Action<BattlePhase> listener) => onPhaseEnded += listener;
        public void UnregisterPhaseEnded(Action<BattlePhase> listener) => onPhaseEnded -= listener;

        public void RegisterTurnAdvanced(Action<int> listener) => onTurnAdvanced += listener;
        public void UnregisterTurnAdvanced(Action<int> listener) => onTurnAdvanced -= listener;

        public void RaisePhaseStarted(BattlePhase phase, int turnNumber) => onPhaseStarted?.Invoke(phase, turnNumber);
        public void RaisePhaseBannerFinished(BattlePhase phase, int turnNumber) => onPhaseBannerFinished?.Invoke(phase, turnNumber);
        public void RaisePhaseEnded(BattlePhase phase) => onPhaseEnded?.Invoke(phase);
        public void RaiseTurnAdvanced(int turnNumber) => onTurnAdvanced?.Invoke(turnNumber);
    }
}
