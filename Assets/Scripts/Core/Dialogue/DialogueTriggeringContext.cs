namespace ProjectAstra.Core.Dialogue
{
    // Where a script is being played from. The context only decides the visual
    // frame and whose game state owns the moment — the text and advance logic
    // are identical everywhere. The prototype uses these two; PreBattle,
    // PostBattle, and Camp join later.
    public enum DialogueTriggeringContext
    {
        // Full-screen story presentation in the Cutscene scene (the opening narrative).
        Cutscene,

        // Overlay over the live battle map; map input is suppressed while it plays.
        BattleMap
    }
}
