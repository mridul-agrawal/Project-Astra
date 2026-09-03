using System.Collections.Generic;

namespace ProjectAstra.Core.Quests
{
    // The mutable half of a condition: which of its targets have been credited so far.
    public sealed class ObjectiveTracker
    {
        private readonly ObjectiveCondition condition;
        private readonly List<string> credited = new();

        public ObjectiveTracker(ObjectiveCondition condition)
        {
            this.condition = condition;
        }

        public int Current => credited.Count;
        public int Required => condition.RequiredCount;
        public bool IsSatisfied => Current >= Required;

        // The exact set, not just the count — a reload has to put the same markers back.
        public IReadOnlyList<string> Credited => credited;

        public bool HasCredited(string targetId) => credited.Contains(targetId);

        // True only when this was a target it wanted and had not already counted. Everything the
        // spec forbids twice — a repeat, a cancelled conversation, a finished set — lands here.
        public bool TryCredit(GameplaySignal signal)
        {
            if (IsSatisfied) return false;
            if (!condition.Matches(signal, out string targetId)) return false;
            if (credited.Contains(targetId)) return false;

            credited.Add(targetId);
            return true;
        }

        // What still needs doing, for markers and for the counter.
        public IEnumerable<string> Outstanding()
        {
            foreach (string target in condition.Targets)
                if (!credited.Contains(target)) yield return target;
        }

        public string MarkerFor(string targetId) => condition.MarkerFor(targetId);

        // Rebuilt from a save. Unknown ids are dropped rather than trusted, so content that changed
        // under an old save cannot inflate the counter past its total.
        public void Restore(IEnumerable<string> targetIds)
        {
            credited.Clear();
            if (targetIds == null) return;

            foreach (string targetId in targetIds)
                if (IsATarget(targetId) && !credited.Contains(targetId))
                    credited.Add(targetId);
        }

        private bool IsATarget(string targetId)
        {
            foreach (string target in condition.Targets)
                if (target == targetId) return true;
            return false;
        }
    }
}
