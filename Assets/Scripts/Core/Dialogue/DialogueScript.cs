using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // An authored conversation: a unique script id plus an ordered list of lines.
    // One asset per scene/exchange, addressable by ScriptId (e.g. "OPENING_CH01").
    [CreateAssetMenu(fileName = "DialogueScript", menuName = "Project Astra/Dialogue/Dialogue Script")]
    public class DialogueScript : ScriptableObject
    {
        [SerializeField] private string _scriptId;
        [SerializeField] private List<DialogueSegment> _segments = new();

        // Legacy flat node list — kept for migration safety + fallback. New scripts
        // author in _segments; the migration tool fills _segments from here.
        [HideInInspector, SerializeField] private List<DialogueNode> _nodes = new();

        [System.NonSerialized] private List<DialogueNode> _flattened;

        public string ScriptId => _scriptId;

        // The runner consumes this. Serve flattened segments when present, else the
        // legacy nodes — so pre-migration data keeps working.
        public IReadOnlyList<DialogueNode> Nodes =>
            _segments != null && _segments.Count > 0 ? Flattened() : _nodes;

        private List<DialogueNode> Flattened()
        {
            if (_flattened != null) return _flattened;
            _flattened = new List<DialogueNode>();
            int id = 0;
            foreach (var segment in _segments)
            {
                if (segment?.Lines == null) continue;
                foreach (var line in segment.Lines)
                    _flattened.Add(DialogueNode.CreateRuntime(id++, line, segment));
            }
            return _flattened;
        }

        // Node ids always mirror list position, so designers never hand-number them
        // when adding or reordering nodes in the inspector.
        private void OnValidate()
        {
            _flattened = null; // refresh the flatten cache after any inspector edit
            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i]?.SetNodeId(i);
        }

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
            script._scriptId = scriptId;
            script._segments = new List<DialogueSegment> { segment };
            return script;
        }

        // Test helper to create a script without needing to create an asset file. Not intended for production use.
        internal static DialogueScript CreateForTest(string scriptId, params DialogueNode[] nodes)
        {
            var script = CreateInstance<DialogueScript>();
            script._scriptId = scriptId;
            script._nodes = new List<DialogueNode>(nodes);
            return script;
        }

        internal static DialogueScript CreateForTestWithSegments(string scriptId, params DialogueSegment[] segments)
        {
            var script = CreateInstance<DialogueScript>();
            script._scriptId = scriptId;
            script._segments = new List<DialogueSegment>(segments);
            return script;
        }
    }
}
