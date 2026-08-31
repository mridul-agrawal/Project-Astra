namespace ProjectAstra.Core.Dialogue.Conversation
{
    // What a conversation needs to remember between runs: whether it has been had before, and
    // which topics have already been asked. Kept as an interface so the runner works anywhere —
    // the hub supplies its visit state, a battle map can supply its own or none at all.
    public interface IConversationMemory
    {
        bool HasCompletedConversation(string conversationId);
        void MarkConversationCompleted(string conversationId);
        bool HasAskedTopic(string conversationId, string optionId);
        void MarkTopicAsked(string conversationId, string optionId);
    }
}
