namespace ProjectAstra.Core.Quests
{
    // The only things a quest event may reach out and touch.
    //
    // Narrow on purpose: the quest system announces what happened and does not decide what the
    // world makes of it. A door that opens on a flag is the door's business, not this.
    public interface IQuestWorld
    {
        void SetFlag(string flagId, bool open);
        void PlayDialogue(string dialogueId);
        void PlayAuthoredEvent(string eventId);
    }
}
