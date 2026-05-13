namespace ProjectAstra.Core.Turn
{
    // Cycles the battle phase Player → Enemy → Allied → Player. When no allies are present
    // the Allied slot is skipped so the loop is Player → Enemy → Player. Pure data class —
    // no Unity dependency — so it can be unit-tested in isolation.
    public class BattlePhaseManager
    {
        private BattlePhase _currentPhase;
        private bool _hasAllies;

        public BattlePhase CurrentPhase => _currentPhase;

        public BattlePhaseManager(bool hasAllies)
        {
            _hasAllies = hasAllies;
            _currentPhase = BattlePhase.PlayerPhase;
        }

        public void SetHasAllies(bool value) => _hasAllies = value;

        public void AdvancePhase()
        {
            _currentPhase = GetNextPhase(_currentPhase);
        }

        public void Reset()
        {
            _currentPhase = BattlePhase.PlayerPhase;
        }

        private BattlePhase GetNextPhase(BattlePhase current) => current switch
        {
            BattlePhase.PlayerPhase => BattlePhase.EnemyPhase,
            BattlePhase.EnemyPhase  => _hasAllies ? BattlePhase.AlliedPhase : BattlePhase.PlayerPhase,
            BattlePhase.AlliedPhase => BattlePhase.PlayerPhase,
            _                       => BattlePhase.PlayerPhase
        };
    }
}
