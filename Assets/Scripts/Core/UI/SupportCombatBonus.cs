namespace ProjectAstra.Core.UI
{
    // SP-02. Aggregated combat bonus from one or more support partners.
    public struct SupportCombatBonus
    {
        public int Atk;
        public int Def;
        public int Hit;
        public int Avo;
        public int Crit;
        public int CritAvo;

        public static readonly SupportCombatBonus Zero = default;
        public bool IsZero => Atk == 0 && Def == 0 && Hit == 0 && Avo == 0 && Crit == 0 && CritAvo == 0;
    }
}
