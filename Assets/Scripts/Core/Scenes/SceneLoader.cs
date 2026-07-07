using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    // Swaps the active Unity scene in response to game state changes. Knows the explicit
    // set of states that have a corresponding scene asset; ignores everything else (overlay
    // states, transient UI states) — those are someone else's problem.
    public class SceneLoader : MonoBehaviour
    {
        private string _currentBaseScene;

        // States that map to an actual scene file under Assets/Scenes/. Anything not in this
        // set is a no-op for SceneLoader (overlays, sub-states, etc.).
        private static readonly HashSet<GameState> SceneStates = new()
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureScreenFader();

            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
                DontDestroyOnLoad(eventSystem.gameObject);
        }

        private void EnsureScreenFader()
        {
            if (ScreenFader.Instance == null)
                new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }

        private void Start()
        {
            EventService.Instance.SubscribeGameStateChanged(OnStateChanged);

            var initialState = GameStateManager.Instance.CurrentState;
            if (SceneStates.Contains(initialState))
            {
                _currentBaseScene = initialState.ToString();
                SceneManager.LoadScene(_currentBaseScene);
            }
        }

        private void OnDestroy()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnStateChanged);
        }

        private void OnStateChanged(StateChangeArgs args)
        {
            if (!SceneStates.Contains(args.NewState)) return;

            string sceneName = args.NewState.ToString();
            if (sceneName == _currentBaseScene) return;

            _currentBaseScene = sceneName;

            if (ScreenFader.Instance != null)
                ScreenFader.Instance.RunTransition(() => SceneManager.LoadScene(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
