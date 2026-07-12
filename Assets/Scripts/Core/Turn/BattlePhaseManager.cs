namespace ProjectAstra.Core.Turn
{
    // Cycles the battle phase Player → Enemy → Allied → Player. When no allies are present
    // the Allied slot is skipped so the loop is Player → Enemy → Player. Pure data class —
    // no Unity dependency — so it can be unit-tested in isolation.
    public class BattlePhaseManager
    {
        private BattlePhase currentPhase;
        private bool hasAllies;

        public BattlePhase CurrentPhase => currentPhase;

        public BattlePhaseManager(bool hasAllies)
        {
            SetHasAllies(hasAllies);
            currentPhase = BattlePhase.PlayerPhase;
        }

        public void SetHasAllies(bool value) => hasAllies = value;

        public void AdvancePhase()
        {
            currentPhase = GetNextPhase(currentPhase);
        }

        public void Reset()
        {
            currentPhase = BattlePhase.PlayerPhase;
        }

        private BattlePhase GetNextPhase(BattlePhase current) => current switch
        {
            BattlePhase.PlayerPhase => BattlePhase.EnemyPhase,
            BattlePhase.EnemyPhase  => hasAllies ? BattlePhase.AlliedPhase : BattlePhase.PlayerPhase,
            BattlePhase.AlliedPhase => BattlePhase.PlayerPhase,
            _                       => BattlePhase.PlayerPhase
        };
    }
}
