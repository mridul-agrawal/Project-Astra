using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Hub
{
    // What has already been said this visit, and which topics she has already raised.
    [Serializable]
    public class HubDialogueMemory : IDialogueMemory
    {
        [SerializeField] private List<string> completedConversationIds = new();
        [SerializeField] private List<string> askedTopics = new();

        // The names the dialogue system asks by.
        public bool HasPlayed(string scriptId) => HasCompletedConversation(scriptId);
        public void MarkPlayed(string scriptId) => MarkConversationCompleted(scriptId);
        public bool HasChosen(string scriptId, string optionId) => HasAskedTopic(scriptId, optionId);
        public void MarkChosen(string scriptId, string optionId) => MarkTopicAsked(scriptId, optionId);

        public bool HasCompletedConversation(string conversationId) =>
            completedConversationIds.Contains(conversationId);

        public void MarkConversationCompleted(string conversationId)
        {
            if (!string.IsNullOrEmpty(conversationId) && !completedConversationIds.Contains(conversationId))
                completedConversationIds.Add(conversationId);
        }

        // Scoped per conversation, because two characters can own a topic of the same name without
        // sharing whether it has been asked.
        public bool HasAskedTopic(string conversationId, string optionId) =>
            askedTopics.Contains(TopicKey(conversationId, optionId));

        public void MarkTopicAsked(string conversationId, string optionId)
        {
            string key = TopicKey(conversationId, optionId);
            if (!string.IsNullOrEmpty(optionId) && !askedTopics.Contains(key)) askedTopics.Add(key);
        }

        private static string TopicKey(string conversationId, string optionId) =>
            conversationId + ":" + optionId;
    }
}
