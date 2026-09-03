using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Quests
{
    // Finished by a named flag going up: a quiz passed, a scripted sequence reaching its end.
    [Serializable]
    public sealed class SignalCondition : ObjectiveCondition
    {
        [Tooltip("Every one of these must be raised. The names are design's, not the code's.")]
        [SerializeField] private string[] signalIds = Array.Empty<string>();

        public override IReadOnlyList<string> Targets => signalIds;

        public override bool Matches(GameplaySignal signal, out string targetId)
        {
            targetId = null;
            if (signal.Kind != GameplaySignalKind.SignalRaised) return false;

            foreach (string id in signalIds)
            {
                if (id != signal.Id) continue;
                targetId = id;
                return true;
            }
            return false;
        }

        public void Configure(params string[] ids) => signalIds = ids ?? Array.Empty<string>();
    }
}
