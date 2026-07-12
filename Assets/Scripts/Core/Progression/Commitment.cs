using UnityEngine;

namespace ProjectAstra.Core.Progression
{
    // Resolution state of a runtime commitment. Stored on save data as the
    // integer value; don't reorder.
    public enum CommitmentResolution { Pending, Kept, Broken }

    // Authored data for a single commitment. Lives under
    // Assets/ScriptableObjects/Commitments/. The asset is immutable at
    // authoring time; runtime state (Pending / Kept / Broken) lives on
    // CommitmentTracker, not on the asset, so chapter replays don't mutate
    // the source.
    [CreateAssetMenu(fileName = "Commitment", menuName = "Project Astra/Progression/Commitment")]
    public class Commitment : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField, TextArea] private string commitmentText;
        [SerializeField] private int chapterIntroduced = 1;

        public string Id => id;
        public string Text => commitmentText;
        public int ChapterIntroduced => chapterIntroduced;
    }
}
