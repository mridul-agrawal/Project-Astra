using System;
using System.Collections.Generic;

namespace ProjectAstra.Core.Quests
{
    // Finished the moment it starts, for a stage whose only job is to run its events.
    [Serializable]
    public sealed class ImmediateCondition : ObjectiveCondition
    {
        private static readonly string[] None = System.Array.Empty<string>();

        public override IReadOnlyList<string> Targets => None;
        public override int RequiredCount => 0;

        public override bool Matches(GameplaySignal signal, out string targetId)
        {
            targetId = null;
            return false;
        }

        // Having no targets is the point here, so the usual authoring check would misfire.
        public override bool IsAuthoredCorrectly(out string problem)
        {
            problem = null;
            return true;
        }
    }
}
