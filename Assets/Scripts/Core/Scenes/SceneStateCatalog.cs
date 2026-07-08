using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    // Designer-owned list of the GameStates that have their own scene file under
    // Assets/Scenes/ (named after the state). SceneLoader loads/swaps only these;
    // every other state (overlays, sub-states) it leaves alone. Editable in the
    // inspector so scenes and states can be added or removed during prototyping
    // without touching code.
    //
    // NOTE: entries are GameState enum values, which Unity stores by number. If you
    // REORDER the GameState enum, re-check this asset — the stored values won't move
    // with it.
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
        };

        // True if this state should load a scene. Linear scan — the list is tiny and
        // only checked on transitions, and staying uncached means a designer's edits
        // in play mode take effect immediately.
        public bool HasScene(GameState state) => sceneStates.Contains(state);
    }
}
