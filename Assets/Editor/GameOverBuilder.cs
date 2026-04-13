using ProjectAstra.Core.UI;
using UnityEditor;

namespace ProjectAstra.EditorTools
{
    // Thin wrapper — see TitleMenuLayoutBuilder for the shared layout.
    // ButtonLabels order matches GameOverUI's runtime wiring:
    //   0 → MainMenu, 1 → SaveMenu.
    public static class GameOverBuilder
    {
        [MenuItem("Project Astra/Build Game Over")]
        public static void Build()
        {
            TitleMenuLayoutBuilder.Build(new TitleMenuConfig
            {
                RootName              = "GameOver",
                TitleText             = "GAME OVER",
                EyebrowText           = "A TACTICAL CHRONICLE",
                FooterHint            = "CHOOSE YOUR CONSTELLATION",
                ButtonLabels          = new[] { "Main Menu", "Save Menu" },
                RuntimeControllerType = typeof(GameOverUI),
            });
        }
    }
}
