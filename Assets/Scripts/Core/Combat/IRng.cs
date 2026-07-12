using System.Collections.Generic;

namespace ProjectAstra.Core.Combat
{
    // Random-number provider abstraction. CombatRound takes IRng instead of
    // calling UnityEngine.Random directly so deterministic tests can pin
    // every roll via FixedRng.
    public interface IRng
    {
        int Range(int minInclusive, int maxExclusive);
    }

    public class UnityRng : IRng
    {
        public int Range(int minInclusive, int maxExclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }
    }

    // Returns the queued values in order; once exhausted, falls back to
    // minInclusive so tests don't crash if they under-supply rolls.
    public class FixedRng : IRng
    {
        private readonly Queue<int> values;

        public FixedRng(params int[] values)
        {
            this.values = new Queue<int>(values);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return values.Count > 0 ? values.Dequeue() : minInclusive;
        }
    }
}
