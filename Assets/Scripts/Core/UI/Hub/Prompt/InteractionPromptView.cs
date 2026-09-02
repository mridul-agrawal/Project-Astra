using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.Hub.Prompt
{
    // Draws the contextual prompt at the bottom of the screen.
    public sealed class InteractionPromptView : MonoBehaviour
    {
        public GameObject content;
        public TextMeshProUGUI verbLabel;
        public TextMeshProUGUI glyphLabel;

        public void Render(InteractionPromptModel model)
        {
            SetVisible(model.Visible);
            if (!model.Visible) return;

            if (verbLabel != null) verbLabel.text = model.Verb;
            if (glyphLabel != null) glyphLabel.text = model.GlyphLabel;
        }

        // Toggles the contents rather than this GameObject, so anything running on it — a fade, a
        // slide — survives being hidden. Same trick the battle objective panel uses.
        public void SetVisible(bool visible)
        {
            if (content != null) content.SetActive(visible);
        }
    }
}
