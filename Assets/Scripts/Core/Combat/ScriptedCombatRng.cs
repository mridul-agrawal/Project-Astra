using System.Collections.Generic;

namespace ProjectAstra.Core.Combat
{
    public enum SwingOutcome { Hit, Miss, Crit }

    // IRng that steers CombatRound swing-by-swing instead of rolling dice. Mirrors the
    // resolver's roll pattern — two hit rolls per swing, then one crit roll only after a
    // landed hit — and serves extreme rolls so every displayed forecast number stays honest.
    // When the outcome queue runs dry, every swing resolves Hit-without-crit: the
    // deterministic default for off-script combat on a fully scripted map.
    public class ScriptedCombatRng : IRng
    {
        // Forcing only works inside CombatRound's short-circuit bounds: a swing with
        // displayed hit 100 cannot miss, and one with displayed crit 0 cannot crit.
        private const int ForcingRoll = 0;
        private const int DenyingRoll = 99;

        private readonly Queue<SwingOutcome> outcomes = new();
        private SwingOutcome currentSwing = SwingOutcome.Hit;
        private int rollIndexInSwing;

        // Replaces any leftover outcomes — queues never carry over between combats.
        public void QueueOutcomes(params SwingOutcome[] swings)
        {
            outcomes.Clear();
            foreach (var swing in swings) outcomes.Enqueue(swing);
            rollIndexInSwing = 0;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            switch (rollIndexInSwing)
            {
                case 0:
                    currentSwing = outcomes.Count > 0 ? outcomes.Dequeue() : SwingOutcome.Hit;
                    rollIndexInSwing = 1;
                    return HitRoll();
                case 1:
                    // A missed swing never asks for a crit roll, so the swing ends here.
                    rollIndexInSwing = currentSwing == SwingOutcome.Miss ? 0 : 2;
                    return HitRoll();
                default:
                    rollIndexInSwing = 0;
                    return currentSwing == SwingOutcome.Crit ? ForcingRoll : DenyingRoll;
            }
        }

        private int HitRoll() => currentSwing == SwingOutcome.Miss ? DenyingRoll : ForcingRoll;
    }
}
