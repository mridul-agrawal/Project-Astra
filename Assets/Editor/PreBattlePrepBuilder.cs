using ProjectAstra.Core.UI;
using UnityEditor;

namespace ProjectAstra.EditorTools
{
    // Thin wrapper — see TitleMenuLayoutBuilder for the shared layout.
    // ButtonLabels order matches PreBattlePrepUI's runtime wiring:
    //   0 → BattleMap.
    public static class PreBattlePrepBuilder
    {
        [MenuItem("Project Astra/Build Pre Battle Prep")]
        public static void Build()
        {
            TitleMenuLayoutBuilder.Build(new TitleMenuConfig
            {
                RootName              = "PreBattlePrep",
                TitleText             = "PRE BATTLE PREP",
                EyebrowText           = "A TACTICAL CHRONICLE",
                FooterHint            = "CHOOSE YOUR CONSTELLATION",
                ButtonLabels          = new[] { "Battle Map" },
                RuntimeControllerType = typeof(PreBattlePrepUI),
            });
        }
    }
}
