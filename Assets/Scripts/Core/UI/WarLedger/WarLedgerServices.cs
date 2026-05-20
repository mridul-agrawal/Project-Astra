using ProjectAstra.Core.Progression;

namespace ProjectAstra.Core.UI.WarLedger
{
    // Simple service-locator for the War's Ledger. Keeps the Ledger's
    // dependency graph explicit without dragging a full DI container into
    // the project. Set by CursorSceneSetup at scene load; consumed by
    // BattleVictoryWatcher and the WarLedgerUI runtime controller.
    public static class WarLedgerServices
    {
        public static ICivilianThreadService CivilianThreadService { get; set; }
            = NullCivilianThreadService.Instance;

        public static IDeathEpitaphProvider EpitaphProvider { get; set; }
            = DefaultDeathEpitaphProvider.Instance;

        // Set by chapter setup when any enemy with IsNamedCommander = true is
        // on the map, regardless of whether they died. Lets the Ledger-trigger
        // predicate show the Ledger even when no named unit died but a named
        // commander was engaged.
        public static bool EnemyForceHadNamedCommanderThisChapter { get; set; } = false;

        public static void ResetForNewChapter()
        {
            EnemyForceHadNamedCommanderThisChapter = false;
        }
    }
}
