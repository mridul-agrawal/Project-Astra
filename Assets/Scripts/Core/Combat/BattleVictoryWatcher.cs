using System;
using UnityEngine;
using ProjectAstra.Core.Progression;
using ProjectAstra.Core.UI;

namespace ProjectAstra.Core
{
    public enum BattleWinner { Player, Enemy, None }
    public enum BattleEndCause { AllEnemiesDead, AllPlayerUnitsDead, Objective, Other }

    public readonly struct BattleConclusion
    {
        public readonly BattleWinner Winner;
        public readonly BattleEndCause Cause;
        public BattleConclusion(BattleWinner w, BattleEndCause c) { Winner = w; Cause = c; }
    }

    /// <summary>
    /// Listens for unit deaths; when all units of one faction are gone, raises
    /// BattleConcluded and requests the game state transition. Target state is
    /// WarLedger when the Ledger-trigger predicate passes, otherwise skip
    /// straight to ChapterClear (spec: "If NONE of [the conditions] are true,
    /// the Ledger is skipped entirely").
    ///
    /// Losses (AllPlayerUnitsDead) route to GameOver — Ledger is strictly a
    /// victory-end-of-chapter screen per the spec.
    /// </summary>
    public class BattleVictoryWatcher : MonoBehaviour
    {
        [SerializeField] private UnitDeathEventChannel _deathChannel;

        public event Action<BattleConclusion> OnBattleConcluded;

        private bool _concluded;

        private void Awake()
        {
            if (_deathChannel != null) _deathChannel.Register(OnUnitDied);
        }

        private void OnDestroy()
        {
            if (_deathChannel != null) _deathChannel.Unregister(OnUnitDied);
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (_concluded) return;

            // UM-02: Lord death pre-empts the faction-wipe path. LordDeathWatcher
            // owns the conclusion (fade → last-words dialogue → GameOver); this
            // watcher steps aside so both handlers don't race to RequestTransition.
            if (args.isLord) { _concluded = true; return; }

            if (TurnManager.Instance == null) return;

            bool anyPlayer = TurnManager.Instance.UnitRegistry.HasUnitsOfFaction(Faction.Player);
            bool anyEnemy  = TurnManager.Instance.UnitRegistry.HasUnitsOfFaction(Faction.Enemy);

            if (!anyEnemy)
            {
                Conclude(new BattleConclusion(BattleWinner.Player, BattleEndCause.AllEnemiesDead));
            }
            else if (!anyPlayer)
            {
                Conclude(new BattleConclusion(BattleWinner.Enemy, BattleEndCause.AllPlayerUnitsDead));
            }
        }

        private void Conclude(BattleConclusion conclusion)
        {
            _concluded = true;
            OnBattleConcluded?.Invoke(conclusion);

            // Let any CommitmentSet evaluators run their predicates against the
            // conclusion (e.g., "did the village burn?") so the tracker is up
            // to date before the Ledger reads it.
            var sets = FindObjectsByType<CommitmentSet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in sets) s.RunEvaluators(conclusion);

            if (conclusion.Winner == BattleWinner.Enemy)
            {
                GameStateManager.Instance?.RequestTransition(GameState.GameOver);
                return;
            }

            // Player victory — decide whether the Ledger is warranted.
            if (ShouldShowLedger())
            {
                GameStateManager.Instance?.RequestTransition(GameState.WarLedger);
            }
            else
            {
                GameStateManager.Instance?.RequestTransition(GameState.ChapterClear);
            }
        }

        private static bool ShouldShowLedger()
        {
            var registry = DeathRegistry.Instance;
            var tracker = CommitmentTracker.Instance;

            bool namedDeath = false;
            if (registry != null)
            {
                foreach (var entry in registry.ForCurrentChapter())
                {
                    if (entry.IsNamed) { namedDeath = true; break; }
                }
            }

            bool commitmentResolved = tracker != null && tracker.ResolvedThisChapter().Count > 0;

            bool civiliansOnMap = (WarLedgerServices.CivilianThreadService
                ?? NullCivilianThreadService.Instance).AnyOnMapThisChapter();

            bool hadNamedCommander = WarLedgerServices.EnemyForceHadNamedCommanderThisChapter;

            return namedDeath || commitmentResolved || civiliansOnMap || hadNamedCommander;
        }
    }
}
