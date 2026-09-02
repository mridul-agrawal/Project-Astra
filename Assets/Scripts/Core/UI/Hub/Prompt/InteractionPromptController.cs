using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
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

        // Only the prompt smooths the target. A press acts on the live one, or she could talk to
        // something she has already turned away from.
        private readonly PromptHysteresis<IInteractable> settling = new();

        private InputDeviceType device = InputDeviceType.Keyboard;
        private IInteractable target;

        public InteractionPromptController(InteractionPromptView view, InputGlyphData glyphData)
        {
            promptView = view;
            promptModel = new InteractionPromptModel();
            glyphs = glyphData;
            Render();
        }

        public void HandleTargetChanged(IInteractable newTarget) => target = newTarget;

        public void Tick(float deltaTime)
        {
            settling.Tick(target, deltaTime);
            Render();
        }

        // Swapping between keyboard and pad changes the glyph, never what the prompt is offering.
        public void HandleDeviceChanged(InputDeviceType newDevice)
        {
            device = newDevice;
            Render();
        }

        // For entering a conversation or changing room, where the prompt should go at once.
        public void Clear()
        {
            target = null;
            settling.Clear();
            Render();
        }

        private void Render()
        {
            IInteractable shown = settling.Current;

            promptModel.Visible = shown != null;
            if (shown != null)
            {
                promptModel.Verb = shown.Verb.ToString();
                promptModel.GlyphLabel = GlyphLabel();
            }
            promptView.Render(promptModel);
        }

        private string GlyphLabel() =>
            glyphs != null ? glyphs.LabelFor(GameInputAction.Confirm, device) : "";
    }
}
