namespace ProjectAstra.Core.Dialogue
{
    // Stable identifier for every cutscene. The single Cutscene scene is reused for all of
    // them; the campaign flow picks which one plays. Resolves to a DialogueScript via
    // CutsceneCatalog. Add a member when you add a cutscene; never reorder existing ones.
    public enum CutsceneId
    {
        None = 0,
        Opening,
    }
}
