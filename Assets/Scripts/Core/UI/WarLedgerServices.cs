using ProjectAstra.Core.Progression;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Simple service-locator for the War's Ledger. Keeps the Ledger's dependency
    /// graph explicit without dragging a full DI container into the project.
    /// Set by CursorSceneSetup at scene load; consumed by BattleVictoryWatcher +
    /// the future WarLedgerUI runtime controller.
    /// </summary>
    public static class WarLedgerServices
    {
        public static ICivilianThreadService CivilianThreadService { get; set; }
            = NullCivilianThreadService.Instance;

        public static IDeathEpitaphProvider EpitaphProvider { get; set; }
            = DefaultDeathEpitaphProvider.Instance;

        /// <summary>
        /// Set by chapter setup when any enemy with isNamedCommander=true is on
        /// the map, regardless of whether they died. Used by the Ledger-trigger
        /// predicate to show the Ledger even if no named unit died but a named
        /// commander was engaged.
        /// </summary>
        public static bool EnemyForceHadNamedCommanderThisChapter { get; set; } = false;

        public static void ResetForNewChapter()
        {
            EnemyForceHadNamedCommanderThisChapter = false;
        }
    }
}
