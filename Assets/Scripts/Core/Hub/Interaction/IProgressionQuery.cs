namespace ProjectAstra.Core.Hub.Interaction
{
    // What an interactable may ask about the player's progress before deciding it is locked.
    //
    // Declared here and implemented over there, so a door can know it is shut without knowing why
    // — nothing in the interaction system learns what an objective is.
    public interface IProgressionQuery
    {
        bool IsGateOpen(string gateId);
        bool IsObjectiveCompleted(string objectiveId);
        bool HasCompletedConversation(string conversationId);
    }
}
