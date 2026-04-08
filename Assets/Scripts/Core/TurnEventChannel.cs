using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(fileName = "TurnEventChannel", menuName = "Project Astra/Core/Turn Event Channel")]
    public class TurnEventChannel : ScriptableObject
    {
        private Action<BattlePhase, int> _onPhaseStarted;
        private Action<BattlePhase> _onPhaseEnded;
        private Action<int> _onTurnAdvanced;

        public void RegisterPhaseStarted(Action<BattlePhase, int> listener) => _onPhaseStarted += listener;
        public void UnregisterPhaseStarted(Action<BattlePhase, int> listener) => _onPhaseStarted -= listener;

        public void RegisterPhaseEnded(Action<BattlePhase> listener) => _onPhaseEnded += listener;
        public void UnregisterPhaseEnded(Action<BattlePhase> listener) => _onPhaseEnded -= listener;

        public void RegisterTurnAdvanced(Action<int> listener) => _onTurnAdvanced += listener;
        public void UnregisterTurnAdvanced(Action<int> listener) => _onTurnAdvanced -= listener;

        public void RaisePhaseStarted(BattlePhase phase, int turnNumber) => _onPhaseStarted?.Invoke(phase, turnNumber);
        public void RaisePhaseEnded(BattlePhase phase) => _onPhaseEnded?.Invoke(phase);
        public void RaiseTurnAdvanced(int turnNumber) => _onTurnAdvanced?.Invoke(turnNumber);
    }
}
