namespace ProjectAstra.Core
{
    /// <summary>Turn-order phases within a battle: Player -> Enemy -> Allied (looping).</summary>
    public enum BattlePhase
    {
        PlayerPhase,
        EnemyPhase,
        AlliedPhase
    }
}
