using System.Collections.Generic;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Progression;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.Combat
{
    // Shared death-handling logic. Split into three pieces so the combat
    // animation playback can run a death animation BETWEEN "HP hits 0" and
    // "victim disappears": call Prepare → play death animation → Hide → Raise.
    // The composite HandleDeath preserves the original instant flow for non-
    // combat callers (poison, environmental, etc.).
    //
    // Order matters and is intentional:
    //   PrepareDeath  — classify victim, build UnitDeathEventArgs, unregister
    //                   from TurnManager (so BattleVictoryWatcher sees correct
    //                   counts when the event fires), clear inventory.
    //   HideVictim    — SetActive(false) on the map sprite. Skipped for Lords
    //                   so LordDeathWatcher can fade them itself.
    //   RaiseDeath    — fire the event on the channel; listeners include
    //                   BattleVictoryWatcher, DeathRegistry, LordDeathWatcher.
    public static class UnitDeathHook
    {
        // Composite — original "instant" behavior for non-combat callers.
        public static void HandleDeath(TestUnit victim, TestUnit killer, UnitDeathEventChannel channel)
        {
            if (victim == null) return;
            var args = PrepareDeath(victim, killer);
            HideVictim(victim);
            RaiseDeath(args, channel);
        }

        // Build the death event args, unregister from the turn registry,
        // and clear the victim's inventory. Does NOT disable the GameObject
        // and does NOT raise the event — the playback caller does those two
        // around its death animation.
        public static UnitDeathEventArgs PrepareDeath(TestUnit victim, TestUnit killer)
        {
            if (victim == null) return default;

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

            // Unregister BEFORE the event fires so listeners (BattleVictoryWatcher)
            // see post-death faction counts. Otherwise "all enemies dead" can
            // never trigger — the last-to-die victim would still appear
            // registered at the moment its death event raises.
            if (TurnManager.Instance != null)
                TurnManager.Instance.UnitRegistry.Unregister(victim);

            if (victim.Inventory != null) victim.Inventory.Clear();

            return args;
        }

        // Disable the map sprite. Lords are skipped so LordDeathWatcher's
        // coroutine has something to fade.
        public static void HideVictim(TestUnit victim)
        {
            if (victim == null) return;
            var def = victim.UnitInstance?.Definition;
            bool lordDeath = victim.isLord || (def != null && def.IsLord);
            if (!lordDeath)
                victim.gameObject.SetActive(false);
        }

        // Fire the death event on the provided channel.
        public static void RaiseDeath(UnitDeathEventArgs args, UnitDeathEventChannel channel)
        {
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
            // Allied NPCs and civilians flow through this branch; there's no
            // separate Civilian faction today, so treat them as named
            // non-enemy deaths.
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
