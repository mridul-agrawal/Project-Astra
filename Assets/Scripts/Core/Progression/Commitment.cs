using UnityEngine;

namespace ProjectAstra.Core.Progression
{
    public enum CommitmentResolution { Pending, Kept, Broken }

    /// <summary>
    /// Authored by the content team under Assets/ScriptableObjects/Commitments/.
    /// The asset is immutable at authoring time; runtime state (Pending/Kept/Broken)
    /// is kept in the CommitmentTracker, not on the asset, so chapter replays
    /// don't mutate the source asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Commitment", menuName = "Project Astra/Progression/Commitment")]
    public class Commitment : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField, TextArea] private string _commitmentText;
        [SerializeField] private int _chapterIntroduced = 1;

        public string Id => _id;
        public string Text => _commitmentText;
        public int ChapterIntroduced => _chapterIntroduced;
    }
}
