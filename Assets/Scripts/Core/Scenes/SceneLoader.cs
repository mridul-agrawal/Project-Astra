using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    public class SceneLoader : MonoBehaviour
    {
        private string currentScene;
        private ScreenFader screenFader;
        [SerializeField] private SceneStateCatalog sceneStateCatalog;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Initialize();
            InitializeSceneFader();
        }
        
        private void Initialize()
        {
            if (sceneStateCatalog == null)
                Debug.LogError("[SceneLoader] Scene State Catalog is not wired — SceneLoader will load no scenes.");

            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
                DontDestroyOnLoad(eventSystem.gameObject);
        }

        private void InitializeSceneFader()
        {
            screenFader = new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }

        private void Start()
        {
            EventService.Instance.SubscribeGameStateChanged(OnStateChanged);

            var initialState = GameStateManager.Instance.CurrentState;
            if (HasSceneFor(initialState))
            {
                currentScene = initialState.ToString();
                LoadScene(currentScene, useFader: false);
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
            if (sceneName == currentScene) return;

            currentScene = sceneName;
            LoadScene(sceneName, useFader: true);
        }

        private bool HasSceneFor(GameState state) => sceneStateCatalog != null && sceneStateCatalog.HasScene(state);

        private void LoadScene(string sceneName, bool useFader)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoader] '{sceneName}' is in the Scene State Catalog but has no matching scene in " +
                    $"Build Settings. Add the scene, or remove {sceneName} from the catalog.");
                return;
            }

            if (useFader && screenFader != null)
                screenFader.RunTransition(() => SceneManager.LoadScene(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
