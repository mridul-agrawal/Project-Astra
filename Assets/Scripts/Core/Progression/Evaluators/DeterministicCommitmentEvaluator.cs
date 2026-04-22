using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Progression.Evaluators
{
    /// <summary>
    /// Dev-test evaluator: resolves a list of commitment IDs to authored Kept/Broken
    /// verdicts when the battle concludes. Not intended for production — content
    /// teams will write real predicates against chapter-outcome flags (did the village
    /// burn? did the NPC survive? etc.). This one just lets us exercise the Ledger's
    /// middle column end-to-end without the full outcome-flag infrastructure.
    ///
    /// Attach to a GameObject, wire into CommitmentSet._evaluators, populate the
    /// <see cref="rules"/> list, and when BattleVictoryWatcher.Conclude runs, each
    /// rule resolves its target commitment.
    /// </summary>
    public class DeterministicCommitmentEvaluator : MonoBehaviour, IChapterOutcomeEvaluator
    {
        [Serializable]
        public struct Rule
        {
            public string commitmentId;
            public CommitmentResolution resolution;
            [Tooltip("If true, only fires when the player won the battle. Otherwise always fires.")]
            public bool onPlayerVictoryOnly;
        }

        [SerializeField] private List<Rule> rules = new();

        public void Evaluate(BattleConclusion conclusion, CommitmentTracker tracker)
        {
            if (tracker == null) return;
            foreach (var r in rules)
            {
                if (r.onPlayerVictoryOnly && conclusion.Winner != BattleWinner.Player) continue;
                tracker.Resolve(r.commitmentId, r.resolution);
            }
        }
    }
}
