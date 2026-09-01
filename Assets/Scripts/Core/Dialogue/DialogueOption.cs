using System;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One selectable option on a Choice node: what it reads, where picking it goes, and
    // whether it should grey out once it has been taken.
    [Serializable]
    public class DialogueOption
    {
        [Tooltip("Stable id for this option, so memory can record that it has been taken.")]
        [SerializeField] private string optionId;

        [SerializeField, TextArea(1, 3)] private string label;

        [Tooltip("Label of the node to continue at when this option is picked.")]
        [SerializeField] private string targetLabel;

        [Tooltip("Grey this out once it has been picked, instead of removing it — a topic menu " +
                 "must not reshuffle under the player between visits.")]
        [SerializeField] private bool askOnce;

        [Tooltip("Where to go instead when this has already been picked. Blank replays the same target.")]
        [SerializeField] private string repeatTargetLabel;

        public string OptionId => optionId;
        public string Label => label;
        public string TargetLabel => targetLabel;
        public bool AskOnce => askOnce;
        public string RepeatTargetLabel => repeatTargetLabel;

        internal static DialogueOption Create(string optionId, string label, string targetLabel,
            bool askOnce = false, string repeatTargetLabel = null)
        {
            return new DialogueOption
            {
                optionId = optionId,
                label = label,
                targetLabel = targetLabel,
                askOnce = askOnce,
                repeatTargetLabel = repeatTargetLabel
            };
        }
    }
}
