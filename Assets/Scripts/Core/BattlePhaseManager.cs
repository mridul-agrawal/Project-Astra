using System;

namespace ProjectAstra.Core
{
    public class BattlePhaseManager
    {
        private BattlePhase _currentPhase;
        private bool _hasAllies;

        public BattlePhase CurrentPhase => _currentPhase;
        public bool HasAllies => _hasAllies;

        public event Action<BattlePhase> OnPhaseChanged;

        public BattlePhaseManager(bool hasAllies)
        {
            _hasAllies = hasAllies;
            _currentPhase = BattlePhase.PlayerPhase;
        }

        public void SetHasAllies(bool value) => _hasAllies = value;

        public void AdvancePhase()
        {
            _currentPhase = GetNextPhase(_currentPhase);
            OnPhaseChanged?.Invoke(_currentPhase);
        }

        public void Reset()
        {
            _currentPhase = BattlePhase.PlayerPhase;
        }

        private BattlePhase GetNextPhase(BattlePhase current)
        {
            return current switch
            {
                BattlePhase.PlayerPhase => BattlePhase.EnemyPhase,
                BattlePhase.EnemyPhase => _hasAllies ? BattlePhase.AlliedPhase : BattlePhase.PlayerPhase,
                BattlePhase.AlliedPhase => BattlePhase.PlayerPhase,
                _ => BattlePhase.PlayerPhase
            };
        }
    }
}
