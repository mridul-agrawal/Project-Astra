using ProjectAstra.Core.UI;
using UnityEditor;

namespace ProjectAstra.EditorTools
{
    // Thin wrapper — delegates the entire layout to TitleMenuLayoutBuilder. The config
    // here is the only place the Main Menu's scene-specific content lives.
    //
    // ButtonLabels order matches MainMenuUI's runtime wiring:
    //   0 → Cutscene, 1 → PreBattlePrep, 2 → BattleMap.
    public static class MainMenuBuilder
    {
        [MenuItem("Project Astra/Build Main Menu")]
        public static void Build()
        {
            TitleMenuLayoutBuilder.Build(new TitleMenuConfig
            {
                RootName              = "MainMenu",
                TitleText             = "PROJECT ASTRA",
                EyebrowText           = "A TACTICAL CHRONICLE",
                FooterHint            = "CHOOSE YOUR CONSTELLATION",
                ButtonLabels          = new[] { "Cutscene", "Pre Battle Prep", "Battle Map" },
                RuntimeControllerType = typeof(MainMenuUI),
            });
        }
    }
}
