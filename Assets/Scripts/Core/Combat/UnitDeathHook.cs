using System.Collections.Generic;
using ProjectAstra.Core.Progression;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Shared death-handling logic called from GridCursor.ApplyCombatResult
    /// (and any future damage source — poison, environmental). Does four things
    /// in order:
    ///   1. Classify the victim (Player / EnemyNamed / EnemyGeneric / Civilian).
    ///   2. Build and raise a UnitDeathEventArgs on the provided channel.
    ///   3. Unregister from TurnManager.UnitRegistry so the victory watcher's
    ///      HasUnitsOfFaction check becomes accurate.
    ///   4. Clear the victim's inventory (items lost, NOT sent to Convoy —
    ///      FE GBA standard per UM-01 spec).
    ///   5. Disable the GameObject so it disappears from the map (existing
    ///      behaviour preserved).
    /// </summary>
    public static class UnitDeathHook
    {
        public static void HandleDeath(TestUnit victim, TestUnit killer, UnitDeathEventChannel channel)
        {
            if (victim == null) return;

            var def = victim.UnitInstance?.Definition;
            var classDef = victim.UnitInstance?.CurrentClass;

            bool lordDeath = victim.isLord || (def != null && def.IsLord);

            var args = new UnitDeathEventArgs
            {
                unitId           = def != null ? def.UnitId : victim.name,
                unitName         = def != null && !string.IsNullOrEmpty(def.UnitName) ? def.UnitName : victim.name,
                className        = classDef != null ? classDef.ClassName : "",
                faction          = Classify(victim, def),
                killerUnitId     = ResolveKillerId(killer),
                causeOfDeath     = CauseOfDeath.Combat,
                chapterNumber    = ChapterContext.CurrentChapterNumber,
                tileCoordinates  = victim.gridPosition,
                activeSupports   = new List<UnitSupportSnapshot>(),
                oneLineIdentity  = def != null ? def.OneLineIdentity : "",
                isLord           = lordDeath,
                victim           = victim,
            };

            // Unregister BEFORE raising so listeners (e.g. BattleVictoryWatcher)
            // see the post-death faction counts — otherwise "all enemies dead"
            // can never be detected, because the last-to-die victim still appears
            // registered at the moment its death event fires.
            if (TurnManager.Instance != null)
                TurnManager.Instance.UnitRegistry.Unregister(victim);

            if (victim.Inventory != null) victim.Inventory.Clear();

            // Lord fade + dialogue + GameOver is driven by LordDeathWatcher; keep
            // the sprite visible so the watcher's coroutine can fade it.
            if (!lordDeath)
                victim.gameObject.SetActive(false);

            channel?.Raise(args);
        }

        private static DeathFaction Classify(TestUnit victim, UnitDefinition def)
        {
            if (victim.faction == Faction.Player) return DeathFaction.Player;
            if (victim.faction == Faction.Enemy)
            {
                bool named = def != null && (def.IsNamedCommander || !string.IsNullOrEmpty(def.UnitId));
                return named ? DeathFaction.EnemyNamed : DeathFaction.EnemyGeneric;
            }
            // Allied NPCs and civilians flow through this branch; today there's
            // no separate Civilian faction, so treat them as named non-enemy deaths.
            return DeathFaction.Civilian;
        }

        private static string ResolveKillerId(TestUnit killer)
        {
            if (killer == null) return null;
            var def = killer.UnitInstance?.Definition;
            return def != null && !string.IsNullOrEmpty(def.UnitId) ? def.UnitId : killer.name;
        }
    }
}
