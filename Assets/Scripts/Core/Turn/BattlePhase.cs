namespace ProjectAstra.Core.Turn
{
    // The three sides that take turns during a battle, in order. The loop is
    // Player → Enemy → (Allied) → Player; Allied is skipped when no allies are present.
    public enum BattlePhase
    {
        PlayerPhase,
        EnemyPhase,
        AlliedPhase
    }
}
