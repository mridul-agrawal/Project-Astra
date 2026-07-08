using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    // Swaps the active Unity scene in response to game state changes.
    // Knows the explicit set of states that have a corresponding scene asset.
    // Ignores everything else (overlay states, transient UI states) — those are someone else's problem.
    public class SceneLoader : MonoBehaviour
    {
        private string currentBaseScene;

        // Designer-owned list of which states have a scene file. Wired to the asset in
        // BootScene — edit that asset (not this script) to add or remove scenes.
        [SerializeField] private SceneStateCatalog sceneCatalog;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureScreenFader();

            if (sceneCatalog == null)
                Debug.LogError("[SceneLoader] Scene State Catalog is not wired — SceneLoader will load no scenes.");

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
            if (HasSceneFor(initialState))
            {
                currentBaseScene = initialState.ToString();
                LoadBaseScene(currentBaseScene, useFader: false);
            }
        }

        private void OnDestroy()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnStateChanged);
        }

        private void OnStateChanged(StateChangeArgs args)
        {
            if (!HasSceneFor(args.NewState)) return;

            string sceneName = args.NewState.ToString();
            if (sceneName == currentBaseScene) return;

            currentBaseScene = sceneName;
            LoadBaseScene(sceneName, useFader: true);
        }

        private bool HasSceneFor(GameState state) => sceneCatalog != null && sceneCatalog.HasScene(state);

        // Swaps in a base scene, fading through black when a ScreenFader exists. Guards
        // against a catalog entry whose scene isn't in Build Settings, so a designer's
        // typo logs a clear error instead of a raw LoadScene exception.
        private void LoadBaseScene(string sceneName, bool useFader)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoader] '{sceneName}' is in the Scene State Catalog but has no matching scene in " +
                    $"Build Settings. Add the scene, or remove {sceneName} from the catalog.");
                return;
            }

            if (useFader && ScreenFader.Instance != null)
                ScreenFader.Instance.RunTransition(() => SceneManager.LoadScene(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
