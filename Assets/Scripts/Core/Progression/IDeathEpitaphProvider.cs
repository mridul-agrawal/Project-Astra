namespace ProjectAstra.Core.Progression
{
    /// <summary>
    /// Resolves the one-line epitaph shown in the Ledger's left column for a
    /// given death. Today we ship a default implementation that returns the
    /// `oneLineIdentity` for named enemies (authored on UnitDefinition) and a
    /// generic "who served faithfully" fallback for player units. The
    /// support-conversation lookup ticket will replace the player-unit branch
    /// with a real pull from the support log.
    /// </summary>
    public interface IDeathEpitaphProvider
    {
        string Resolve(UnitDeathEventArgs args);
    }

    public sealed class DefaultDeathEpitaphProvider : IDeathEpitaphProvider
    {
        public static readonly DefaultDeathEpitaphProvider Instance = new DefaultDeathEpitaphProvider();

        public string Resolve(UnitDeathEventArgs args)
        {
            // Named enemies carry their identity line on UnitDefinition; the
            // event copies it into the payload at death time.
            if (args.faction == DeathFaction.EnemyNamed && !string.IsNullOrEmpty(args.oneLineIdentity))
                return args.oneLineIdentity;

            // Civilian deaths route through the civilian thread service, but
            // if one ever surfaces via the generic death path we fall through
            // to a name-only line.
            if (args.faction == DeathFaction.Civilian)
                return string.IsNullOrEmpty(args.oneLineIdentity)
                    ? $"{args.unitName}, among the fallen."
                    : args.oneLineIdentity;

            // Generic enemies don't get an epitaph — they feed the unnamed tail.
            if (args.faction == DeathFaction.EnemyGeneric)
                return "";

            // Player units — until the support-log lookup ships, use the
            // default line. Spec: `[Name], who served faithfully.`
            return $"{args.unitName}, who served faithfully.";
        }
    }
}
