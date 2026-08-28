using System;
using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Gurukul.Conversation
{
    // Append only — authored nodes store the kind as an int.
    public enum ConversationNodeKind
    {
        // Plays one linear DialogueScript, then follows nextNodeId.
        Script,

        // Presents options and jumps to the chosen one's target.
        Choice,

        // Like a choice, but remembers which topics have been asked and can be reopened. Each
        // topic's response points back here.
        TopicMenu,

        // Records something in the visit's runtime state — a conversation completed, a quiz passed.
        SetFlag,

        End
    }

    [Serializable]
    public class ConversationOption
    {
        [Tooltip("Stable id, so an asked topic stays asked even if the label is reworded.")]
        public string optionId;

        public string label;
        public string nextNodeId;

        [Tooltip("Topic menus only: grey this out once it has been asked.")]
        public bool askOnce;

        [Tooltip("Topic menus only: played instead once it has been asked. Leave empty to keep the original response.")]
        public string repeatNodeId;
    }

    [Serializable]
    public class ConversationNode
    {
        public string nodeId;
        public ConversationNodeKind kind;

        [Header("Script")]
        public DialogueScript script;

        [Header("Script / SetFlag")]
        public string nextNodeId;

        [Header("Choice / TopicMenu")]
        public ConversationOption[] options = Array.Empty<ConversationOption>();

        [Tooltip("Off for anything that must be answered — a knowledge check can't be backed out of. On for something optional, like a departure confirmation.")]
        public bool allowCancel = true;

        [Tooltip("Choice / TopicMenu: where cancelling goes. Leave empty to end the conversation.")]
        public string cancelNodeId;

        [Header("SetFlag")]
        public string flagId;
    }

    // An authored conversation: an ordered set of beats with the branches between them.
    //
    // Sits above the dialogue system rather than inside it. DialogueScript is a flat list of lines
    // with no branching, and its runner and view are pinned by a test suite — so choices, topics and
    // quizzes are expressed by sequencing whole scripts instead of by reaching into them. A quiz
    // needs no machinery of its own: it is a choice whose wrong answers point back at the question.
    [CreateAssetMenu(fileName = "ConversationGraph", menuName = "Project Astra/Gurukul/Conversation")]
    public class ConversationGraph : ScriptableObject
    {
        [SerializeField] private string conversationId;
        [SerializeField] private string entryNodeId;

        [Tooltip("Where a repeat visit starts. Leave empty and a repeat replays the first-time entry.")]
        [SerializeField] private string repeatEntryNodeId;

        [SerializeField] private ConversationNode[] nodes = Array.Empty<ConversationNode>();

        public string ConversationId => conversationId;
        public string EntryNodeId => entryNodeId;
        public string RepeatEntryNodeId => repeatEntryNodeId;
        public ConversationNode[] Nodes => nodes;

        public string EntryFor(bool alreadyCompleted) =>
            alreadyCompleted && !string.IsNullOrEmpty(repeatEntryNodeId) ? repeatEntryNodeId : entryNodeId;

        public ConversationNode Find(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            foreach (ConversationNode node in nodes)
                if (node != null && node.nodeId == nodeId) return node;
            return null;
        }

        internal static ConversationGraph CreateForTest(string conversationId, string entryNodeId,
            ConversationNode[] nodes, string repeatEntryNodeId = null)
        {
            var graph = CreateInstance<ConversationGraph>();
            graph.conversationId = conversationId;
            graph.entryNodeId = entryNodeId;
            graph.repeatEntryNodeId = repeatEntryNodeId;
            graph.nodes = nodes;
            return graph;
        }
    }
}
