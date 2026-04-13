using ProjectAstra.Core.UI;
using UnityEditor;

namespace ProjectAstra.EditorTools
{
    // Thin wrapper — see TitleMenuLayoutBuilder for the shared layout.
    // ButtonLabels order matches CutsceneUI's runtime wiring:
    //   0 → PreBattlePrep, 1 → BattleMap.
    public static class CutsceneBuilder
    {
        [MenuItem("Project Astra/Build Cutscene")]
        public static void Build()
        {
            TitleMenuLayoutBuilder.Build(new TitleMenuConfig
            {
                RootName              = "Cutscene",
                TitleText             = "CUTSCENE",
                EyebrowText           = "A TACTICAL CHRONICLE",
                FooterHint            = "CHOOSE YOUR CONSTELLATION",
                ButtonLabels          = new[] { "Pre Battle Prep", "Battle Map" },
                RuntimeControllerType = typeof(CutsceneUI),
            });
        }
    }
}
