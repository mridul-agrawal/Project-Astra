using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul.Conversation
{
    // Walks one conversation graph: plays a script, waits, shows a choice, follows the branch, and
    // stops when it runs out of nodes.
    //
    // Pure C# behind IConversationPresenter, so a quiz's wrong-answer loop and a topic menu's
    // greying both test without a Canvas — the same arrangement DialogueRunner uses.
    public class GurukulConversationRunner
    {
        private readonly ConversationGraph graph;
        private readonly IConversationPresenter presenter;
        private readonly GurukulRuntimeState state;
        private readonly List<ConversationChoice> choiceBuffer = new();

        private ConversationNode current;
        private bool running;

        // Raised for each SetFlag node, so the visit's progression can react without this class
        // knowing what an objective is.
        public event Action<string> FlagRaised;
        public event Action Completed;

        public GurukulConversationRunner(ConversationGraph graph, IConversationPresenter presenter,
            GurukulRuntimeState state)
        {
            this.graph = graph;
            this.presenter = presenter;
            this.state = state;
        }

        public bool IsRunning => running;
        public string CurrentNodeId => current?.nodeId;

        public void Begin()
        {
            if (graph == null)
            {
                Debug.LogError("[GurukulConversation] Asked to run a conversation with no graph.");
                return;
            }

            running = true;
            presenter.BeginConversation();

            bool seenBefore = state != null && state.HasCompletedConversation(graph.ConversationId);
            GoTo(graph.EntryFor(seenBefore));
        }

        private void GoTo(string nodeId)
        {
            current = graph.Find(nodeId);
            if (current == null)
            {
                Finish();
                return;
            }
            Enter(current);
        }

        private void Enter(ConversationNode node)
        {
            switch (node.kind)
            {
                case ConversationNodeKind.Script: PlayScript(node); break;
                case ConversationNodeKind.Choice: ShowChoices(node, isTopicMenu: false); break;
                case ConversationNodeKind.TopicMenu: ShowChoices(node, isTopicMenu: true); break;
                case ConversationNodeKind.SetFlag: RaiseFlag(node); break;
                default: Finish(); break;
            }
        }

        private void PlayScript(ConversationNode node)
        {
            if (node.script == null)
            {
                Debug.LogError($"[GurukulConversation] '{graph.ConversationId}' node '{node.nodeId}' has no script.");
                Finish();
                return;
            }
            presenter.PlayScript(node.script, () => GoTo(node.nextNodeId));
        }

        private void RaiseFlag(ConversationNode node)
        {
            FlagRaised?.Invoke(node.flagId);
            GoTo(node.nextNodeId);
        }

        private void ShowChoices(ConversationNode node, bool isTopicMenu)
        {
            if (node.options == null || node.options.Length == 0)
            {
                Debug.LogError($"[GurukulConversation] '{graph.ConversationId}' node '{node.nodeId}' offers no options.");
                Finish();
                return;
            }

            BuildChoices(node, isTopicMenu);
            presenter.ShowChoices(choiceBuffer, node.allowCancel,
                index => Choose(node, index, isTopicMenu),
                () => GoTo(node.cancelNodeId));
        }

        // A topic already raised is shown greyed rather than removed, so the menu doesn't reshuffle
        // under the player between visits.
        private void BuildChoices(ConversationNode node, bool isTopicMenu)
        {
            choiceBuffer.Clear();
            foreach (ConversationOption option in node.options)
            {
                bool used = isTopicMenu && option.askOnce && HasAsked(option);
                choiceBuffer.Add(new ConversationChoice(option.label, !used));
            }
        }

        private void Choose(ConversationNode node, int index, bool isTopicMenu)
        {
            if (index < 0 || index >= node.options.Length)
            {
                Finish();
                return;
            }

            ConversationOption option = node.options[index];

            // Read before marking, or the very first pick would already count as a repeat.
            bool askedBefore = isTopicMenu && HasAsked(option);
            if (isTopicMenu) state?.MarkTopicAsked(graph.ConversationId, option.optionId);

            bool useRepeat = askedBefore && !string.IsNullOrEmpty(option.repeatNodeId);
            GoTo(useRepeat ? option.repeatNodeId : option.nextNodeId);
        }

        private bool HasAsked(ConversationOption option) =>
            state != null && state.HasAskedTopic(graph.ConversationId, option.optionId);

        private void Finish()
        {
            if (!running) return;
            running = false;
            current = null;

            state?.MarkConversationCompleted(graph.ConversationId);
            presenter.EndConversation();
            Completed?.Invoke();
        }
    }
}
