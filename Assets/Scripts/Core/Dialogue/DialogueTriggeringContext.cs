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

        // Inside the Gurukul hub. The hub already owns input and its own sub-state, so the service
        // neither takes the game state nor binds the confirm button here — the hub's router feeds
        // it instead, which is what keeps one press from being read as two things.
        Gurukul
    }
}
