using System;
using System.Collections.Generic;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Gurukul.Conversation
{
    // One selectable line, already resolved: what it reads and whether it can still be picked.
    public readonly struct ConversationChoice
    {
        public readonly string Label;
        public readonly bool Enabled;

        public ConversationChoice(string label, bool enabled)
        {
            Label = label;
            Enabled = enabled;
        }
    }

    // Everything the conversation runner needs the world to do. Kept as an interface so the runner
    // is engine-free and every graph shape — retry loops included — tests against a fake.
    public interface IConversationPresenter
    {
        void BeginConversation();
        void PlayScript(DialogueScript script, Action onFinished);
        void ShowChoices(IReadOnlyList<ConversationChoice> choices, bool allowCancel,
            Action<int> onChosen, Action onCancelled);
        void EndConversation();
    }
}
