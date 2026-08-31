namespace ProjectAstra.Core.Dialogue
{
    // Where a script is being played from. The context only decides the visual
    // frame and whose game state owns the moment — the text and advance logic
    // are identical everywhere. PreBattle and PostBattle join later.
    public enum DialogueTriggeringContext
    {
        // Full-screen story presentation in the Cutscene scene (the opening narrative).
        Cutscene,

        // Overlay over the live battle map; map input is suppressed while it plays.
        BattleMap,

        // One script inside a longer branching conversation. ConversationPlayer owns the game state
        // and the confirm button for the whole exchange, so the service takes neither here — which
        // is what keeps one press from being read as both "advance the line" and "pick this option".
        Conversation
    }
}
