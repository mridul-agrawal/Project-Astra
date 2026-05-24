namespace ProjectAstra.Core.Dialogue
{
    // Which portrait variant a speaker shows on a given line. A speaker that
    // hasn't authored a sprite for the chosen expression falls back to Neutral.
    // The prototype only needs this handful; the full spec's CUSTOM_01..05 land later.
    public enum DialogueExpression
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Determined,
        Afraid
    }
}
