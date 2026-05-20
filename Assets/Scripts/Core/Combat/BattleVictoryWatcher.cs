using System;
using UnityEngine;
using ProjectAstra.Core.Progression;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.WarLedger;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat
{
    public enum BattleWinner { Player, Enemy, None }
    public enum BattleEndCause { AllEnemiesDead, AllPlayerUnitsDead, Objective, Other }

    public readonly struct BattleConclusion
    {
        public readonly BattleWinner Winner;
        public readonly BattleEndCause Cause;
        public BattleConclusion(BattleWinner w, BattleEndCause c) { Winner = w; Cause = c; }
    }

    // Listens for unit deaths and concludes the chapter when one faction has
    // no units left. Player victories transition to WarLedger (if the
    // Ledger-trigger predicate passes) or skip straight to ChapterClear;
    // player losses transition to GameOver — the Ledger is a victory-only
    // screen per spec.
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
            // owns the conclusion (fade → last-words dialogue → GameOver);
            // step aside so both handlers don't race to RequestTransition.
            if (args.isLord) { _concluded = true; return; }

            if (TurnManager.Instance == null) return;

            bool anyPlayer = TurnManager.Instance.UnitRegistry.HasUnitsOfFaction(Faction.Player);
            bool anyEnemy  = TurnManager.Instance.UnitRegistry.HasUnitsOfFaction(Faction.Enemy);

            if (!anyEnemy)
                Conclude(new BattleConclusion(BattleWinner.Player, BattleEndCause.AllEnemiesDead));
            else if (!anyPlayer)
                Conclude(new BattleConclusion(BattleWinner.Enemy, BattleEndCause.AllPlayerUnitsDead));
        }

        private void Conclude(BattleConclusion conclusion)
        {
            _concluded = true;
            OnBattleConcluded?.Invoke(conclusion);

            // Run any CommitmentSet evaluators against the conclusion (e.g.
            // "did the village burn?") so the tracker is up to date before
            // the Ledger reads it.
            var sets = FindObjectsByType<CommitmentSet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in sets) s.RunEvaluators(conclusion);

            if (conclusion.Winner == BattleWinner.Enemy)
            {
                GameStateManager.Instance?.RequestTransition(GameState.GameOver);
                return;
            }

            // Player victory: pick Ledger vs ChapterClear.
            GameStateManager.Instance?.RequestTransition(
                ShouldShowLedger() ? GameState.WarLedger : GameState.ChapterClear);
        }

        // Spec: show the Ledger only when at least one of these is true.
        // Otherwise skip straight to ChapterClear.
        private static bool ShouldShowLedger()
        {
            if (HasNamedDeathThisChapter()) return true;
            if (HasResolvedCommitmentThisChapter()) return true;
            if (HasCiviliansOnMapThisChapter()) return true;
            if (WarLedgerServices.EnemyForceHadNamedCommanderThisChapter) return true;
            return false;
        }

        private static bool HasNamedDeathThisChapter()
        {
            var registry = DeathRegistry.Instance;
            if (registry == null) return false;

            foreach (var entry in registry.ForCurrentChapter())
                if (entry.IsNamed) return true;
            return false;
        }

        private static bool HasResolvedCommitmentThisChapter()
        {
            var tracker = CommitmentTracker.Instance;
            return tracker != null && tracker.ResolvedThisChapter().Count > 0;
        }

        private static bool HasCiviliansOnMapThisChapter()
        {
            var service = WarLedgerServices.CivilianThreadService ?? NullCivilianThreadService.Instance;
            return service.AnyOnMapThisChapter();
        }
    }
}
