using ProjectAstra.Core.UI;
using UnityEditor;

namespace ProjectAstra.EditorTools
{
    // Thin wrapper — see TitleMenuLayoutBuilder for the shared layout.
    // ButtonLabels order matches ChapterClearUI's runtime wiring:
    //   0 → Cutscene, 1 → SaveMenu.
    public static class ChapterClearBuilder
    {
        [MenuItem("Project Astra/Build Chapter Clear")]
        public static void Build()
        {
            TitleMenuLayoutBuilder.Build(new TitleMenuConfig
            {
                RootName              = "ChapterClear",
                TitleText             = "CHAPTER CLEAR",
                EyebrowText           = "A TACTICAL CHRONICLE",
                FooterHint            = "CHOOSE YOUR CONSTELLATION",
                ButtonLabels          = new[] { "Cutscene", "Save Menu" },
                RuntimeControllerType = typeof(ChapterClearUI),
            });
        }
    }
}
