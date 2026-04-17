namespace ProjectAstra.Core.UI
{
    // SP-02. Returns the aggregated support combat bonus currently active on a
    // unit (sum across all partners in range). Also exposes a per-pair query used
    // by the Support Detail sub-panel.
    //
    // Stub implementation returns zeros. Real implementation lives in the SP-02
    // support bonus ticket.
    public interface ISupportBonusProvider
    {
        SupportCombatBonus GetActiveBonus(UnitInstance unit);
        SupportCombatBonus GetPairBonus(UnitInstance unit, UnitDefinition partner, BondStage stage);
    }
}
