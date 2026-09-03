using System;
using System.Collections.Generic;

namespace ProjectAstra.Core.Quests
{
    // What finishes an objective. One class per kind; the runner never asks which kind this is.
    [Serializable]
    public abstract class ObjectiveCondition
    {
        // Every target this condition is waiting on, in authored order.
        public abstract IReadOnlyList<string> Targets { get; }

        // How many distinct targets must be credited. A set of them is the 0/5 counter.
        public virtual int RequiredCount => Targets.Count;

        // Does this signal count, and under which id should it be remembered? The id is what makes
        // the completed-target set, so it has to name the target rather than the signal.
        public abstract bool Matches(GameplaySignal signal, out string targetId);

        // Who a marker should stand over while this target is outstanding. Empty for a condition
        // with nothing in the world to point at, like a raised flag.
        public virtual string MarkerFor(string targetId) => null;

        public ObjectiveTracker CreateTracker() => new(this);

        // Run by the content tooling. A stage nobody can finish strands the visit with no error.
        public virtual bool IsAuthoredCorrectly(out string problem)
        {
            problem = null;
            if (RequiredCount != 0) return true;

            problem = $"{GetType().Name} has no targets, so it can never complete";
            return false;
        }
    }
}
