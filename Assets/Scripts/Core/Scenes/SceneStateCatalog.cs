using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    // Designer-owned list of the GameStates that have their own scene file under
    // Assets/Scenes/ (named after the state).
    [CreateAssetMenu(fileName = "SceneStateCatalog", menuName = "Project Astra/Core/Scene State Catalog")]
    public class SceneStateCatalog : ScriptableObject
    {
        [Tooltip("Game states that have a matching scene file in Assets/Scenes/ and Build Settings. " +
                 "Each state's scene must be named exactly after the state (e.g. BattleMap -> BattleMap.unity).")]
        [SerializeField] private List<GameState> sceneStates = new()
        {
            GameState.Splash,
            GameState.TitleScreen,
            GameState.MainMenu,
            GameState.Cutscene,
            GameState.PreBattlePrep,
            GameState.BattleMap,
            GameState.ChapterClear,
            GameState.GameOver,
            GameState.HubExploration,
        };

        public bool HasScene(GameState state) => sceneStates.Contains(state);
    }
}
