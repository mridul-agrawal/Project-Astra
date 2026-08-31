using System;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // One stage of a hub visit: the line the player reads, what finishes it, and what changes about
    // the world when it does. Its own asset because content iterates on these constantly and a
    // visit that owned them inline would be an unmergeable wall of YAML.
    [CreateAssetMenu(fileName = "GurukulObjectiveData", menuName = "Project Astra/Gurukul/Objective Data")]
    public class GurukulObjectiveData : ScriptableObject
    {
        [SerializeField] private string objectiveId;

        [Tooltip("Short and action-oriented, e.g. \"Talk to the other students\".")]
        [SerializeField] private string displayText;

        [SerializeField] private GurukulCondition completion = new();

        [Tooltip("Applied once, in order, the moment the objective completes.")]
        [SerializeField] private GurukulEffect[] onComplete = Array.Empty<GurukulEffect>();

        [Tooltip("Characters, objects or doors that get a marker while this objective is active. A target drops its marker as soon as it is satisfied.")]
        [SerializeField] private string[] markerTargetIds = Array.Empty<string>();

        public string ObjectiveId => objectiveId;
        public string DisplayText => displayText;
        public GurukulCondition Completion => completion;
        public GurukulEffect[] OnComplete => onComplete;
        public string[] MarkerTargetIds => markerTargetIds;

        // Builds an objective without an asset file, so the progression tests don't need a fixture
        // folder. Not for production use.
        internal static GurukulObjectiveData CreateForTest(string objectiveId, GurukulCondition completion,
            GurukulEffect[] onComplete = null, string[] markerTargetIds = null, string displayText = "Do the thing")
        {
            var objective = CreateInstance<GurukulObjectiveData>();
            objective.objectiveId = objectiveId;
            objective.displayText = displayText;
            objective.completion = completion;
            objective.onComplete = onComplete ?? Array.Empty<GurukulEffect>();
            objective.markerTargetIds = markerTargetIds ?? Array.Empty<string>();
            return objective;
        }
    }
}
