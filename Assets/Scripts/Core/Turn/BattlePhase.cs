namespace ProjectAstra.Core.Turn
{
    /// <summary>Turn-order phases within a battle: Player -> Enemy -> Allied (looping).</summary>
    public enum BattlePhase
    {
        PlayerPhase,
        EnemyPhase,
        AlliedPhase
    }
}
