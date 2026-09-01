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

        // A conversation in a world that is already loaded — the hub, or a battle map. Plays over
        // whatever is on screen; the service takes the Dialogue state and hands it back after.
        Conversation
    }
}
