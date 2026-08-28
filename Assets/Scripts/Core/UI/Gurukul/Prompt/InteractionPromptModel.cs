namespace ProjectAstra.Core.UI.Gurukul.Prompt
{
    // What the interaction prompt is showing: one verb and the button that performs it.
    public sealed class InteractionPromptModel
    {
        public bool Visible;
        public string Verb = "";
        public string GlyphLabel = "";
    }
}
