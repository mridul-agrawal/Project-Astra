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
        [SerializeField] private List<DialogueNode> _nodes = new();

        public string ScriptId => _scriptId;
        public IReadOnlyList<DialogueNode> Nodes => _nodes;

        // Node ids always mirror list position, so designers never hand-number them
        // when adding or reordering nodes in the inspector.
        private void OnValidate()
        {
            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i]?.SetNodeId(i);
        }

        // Test helper to create a script without needing to create an asset file. Not intended for production use.
        internal static DialogueScript CreateForTest(string scriptId, params DialogueNode[] nodes)
        {
            var script = CreateInstance<DialogueScript>();
            script._scriptId = scriptId;
            script._nodes = new List<DialogueNode>(nodes);
            return script;
        }
    }
}
