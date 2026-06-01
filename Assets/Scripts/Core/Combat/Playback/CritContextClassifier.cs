using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat.Playback
{
    // Decides whether a crit lands as Righteous or Tragic. Called by the brave
    // flow composer, which walks the hit list once with a running HP tally so
    // it knows which crit (if any) is the fatal blow on each side.
    public static class CritContextClassifier
    {
        public static CritContext Classify(TestUnit victim, bool willKill)
        {
            if (!victim || !willKill) return CritContext.Righteous;
            var def = victim.UnitDefinition;
            if (def == null) return CritContext.Righteous;

            if (def.IsLord || def.IsNamedCommander) return CritContext.Tragic;
            if (victim.faction == Faction.Player)    return CritContext.Tragic;

            // TODO(RC-04): if (RecruitTargetRegistry.IsRecruitable(def.UnitId)) return Tragic;
            return CritContext.Righteous;
        }
    }
}
