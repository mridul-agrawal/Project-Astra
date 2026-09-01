using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // An authored conversation: a unique script id plus an ordered list of lines.
    // One asset per scene/exchange, addressable by ScriptId (e.g. "OPENING_CH01").
    [CreateAssetMenu(fileName = "DialogueScript", menuName = "Project Astra/Dialogue/Dialogue Script")]
    public class DialogueScript : ScriptableObject
    {
        [SerializeField] private string scriptId;
        [SerializeField] private List<DialogueSegment> segments = new();

        [Tooltip("Where to start when memory says this script has been played before. Blank " +
                 "replays it from the top, which is what a script with no repeat content wants.")]
        [SerializeField] private string repeatEntryLabel;

        // Filled by flattening the segments, or seeded directly by a test.
        [System.NonSerialized] private List<DialogueNode> flattened;

        public string ScriptId => scriptId;
        public string RepeatEntryLabel => repeatEntryLabel;

        // The runner consumes this: every authored line, with its segment's background,
        // speed and auto-advance already folded in.
        public IReadOnlyList<DialogueNode> Nodes => Flattened();

        // Where a Jump or a Choice target lands. Returns -1 for a label nothing carries, which
        // the runner reports rather than silently running on into the next line.
        public int IndexOfLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return -1;

            IReadOnlyList<DialogueNode> all = Nodes;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Label == label) return i;
            return -1;
        }

        private List<DialogueNode> Flattened()
        {
            if (flattened != null) return flattened;
            flattened = new List<DialogueNode>();
            if (segments == null) return flattened;

            int id = 0;
            foreach (var segment in segments)
            {
                if (segment?.Lines == null) continue;
                foreach (var line in segment.Lines)
                    flattened.Add(DialogueNode.CreateRuntime(id++, line, segment));
            }
            return flattened;
        }

        // Node ids mirror position in the flattened list, so designers never hand-number
        // them when adding or reordering lines in the inspector.
        private void OnValidate() => flattened = null;

        // Builds a one-segment script from code at runtime (no asset) for dynamic,
        // data-driven dialogue like a unit's last words. An empty/unset speaker falls
        // back to the narrator so the runner shows the text instead of skipping it.
        public static DialogueScript CreateRuntime(string scriptId, string speakerId,
            IReadOnlyList<string> lines, float textSpeed = -1f, float autoAdvanceDelay = 0f,
            PortraitPosition position = PortraitPosition.Left)
        {
            string resolvedSpeaker = string.IsNullOrEmpty(speakerId)
                ? DialogueSpeakerRegistry.NarratorId
                : speakerId;

            // Portraits face inward toward the text: a Left portrait looks Right; others
            // keep the art's native Left facing.
            PortraitFacing facing = position == PortraitPosition.Left
                ? PortraitFacing.Right
                : PortraitFacing.Left;

            int count = lines?.Count ?? 0;
            var built = new DialogueLine[count];
            for (int i = 0; i < count; i++)
                built[i] = DialogueLine.Create(resolvedSpeaker, lines[i], position: position, facing: facing);

            var segment = DialogueSegment.Create(null, textSpeed, autoAdvanceDelay, built);

            var script = CreateInstance<DialogueScript>();
            script.scriptId = scriptId;
            script.segments = new List<DialogueSegment> { segment };
            return script;
        }

        // Test helper to create a script without needing an asset file. Seeds the flattened
        // list directly, which is what the runner reads. Not intended for production use.
        internal static DialogueScript CreateForTest(string scriptId, params DialogueNode[] nodes)
        {
            var script = CreateInstance<DialogueScript>();
            script.scriptId = scriptId;
            script.flattened = new List<DialogueNode>(nodes);
            return script;
        }

        internal static DialogueScript CreateForTestWithSegments(string scriptId, params DialogueSegment[] segments)
        {
            var script = CreateInstance<DialogueScript>();
            script.scriptId = scriptId;
            script.segments = new List<DialogueSegment>(segments);
            return script;
        }

        internal static DialogueScript CreateForTest(string scriptId, string repeatEntryLabel,
            params DialogueNode[] nodes)
        {
            var script = CreateForTest(scriptId, nodes);
            script.repeatEntryLabel = repeatEntryLabel;
            return script;
        }
    }
}
