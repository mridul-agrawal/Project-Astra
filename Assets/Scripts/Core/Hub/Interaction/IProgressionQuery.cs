namespace ProjectAstra.Core.Hub.Interaction
{
    // What an interactable may ask about the player's progress before deciding it is locked.
    public interface IProgressionQuery
    {
        bool IsGateOpen(string gateId);
        bool IsObjectiveCompleted(string objectiveId);
        bool HasCompletedConversation(string conversationId);
    }
}
