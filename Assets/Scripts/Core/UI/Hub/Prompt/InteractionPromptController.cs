using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.UI.Hub.Prompt
{
    // Turns "she is standing in front of this" into the line the prompt shows. Plain C# — the view
    // owns the pixels, this owns what they say.
    public sealed class InteractionPromptController
    {
        public InteractionPromptView promptView;
        public InteractionPromptModel promptModel;

        private readonly InputGlyphData glyphs;
        private InputDeviceType device = InputDeviceType.Keyboard;
        private HubVerb? verb;

        public InteractionPromptController(InteractionPromptView view, InputGlyphData glyphData)
        {
            promptView = view;
            promptModel = new InteractionPromptModel();
            glyphs = glyphData;
            Render();
        }

        public void HandleTargetChanged(HubInteractionCandidate? target)
        {
            verb = target?.Verb;
            Render();
        }

        // Swapping between keyboard and pad changes the glyph, never what the prompt is offering.
        public void HandleDeviceChanged(InputDeviceType newDevice)
        {
            device = newDevice;
            Render();
        }

        private void Render()
        {
            promptModel.Visible = verb.HasValue;
            if (verb.HasValue)
            {
                promptModel.Verb = verb.Value.ToString();
                promptModel.GlyphLabel = GlyphLabel();
            }
            promptView.Render(promptModel);
        }

        private string GlyphLabel() =>
            glyphs != null ? glyphs.LabelFor(GameInputAction.Confirm, device) : "";
    }
}
