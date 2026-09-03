using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Quests
{
    // Finished by looking at things. The interactable's own id is both the target and the marker.
    [Serializable]
    public sealed class InspectCondition : ObjectiveCondition
    {
        [Tooltip("Every one of these must be inspected. One id is a single object; several make a set.")]
        [HubPick(HubIdKind.Interactable)]
        [SerializeField] private string[] interactableIds = Array.Empty<string>();

        public override IReadOnlyList<string> Targets => interactableIds;

        public override bool Matches(GameplaySignal signal, out string targetId)
        {
            targetId = null;
            if (signal.Kind != GameplaySignalKind.ObjectInspected) return false;

            foreach (string id in interactableIds)
            {
                if (id != signal.Id) continue;
                targetId = id;
                return true;
            }
            return false;
        }

        public override string MarkerFor(string targetId) => targetId;

        public void Configure(params string[] ids) => interactableIds = ids ?? Array.Empty<string>();
    }
}
