using System.Collections.Generic;

namespace ProjectAstra.Core
{
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

    public class FixedRng : IRng
    {
        private readonly Queue<int> _values;

        public FixedRng(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return _values.Count > 0 ? _values.Dequeue() : minInclusive;
        }
    }
}
