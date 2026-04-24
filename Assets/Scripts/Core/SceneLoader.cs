using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Listens to state changes and loads base scenes or instantiates overlay prefabs accordingly.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        private string _currentBaseScene;
        private GameObject _activeOverlay;

        private static readonly HashSet<GameState> OverlayStates = new()
        {
            GameState.BattleMapPaused,
            GameState.CombatAnimation,
            GameState.Dialogue,
            GameState.SaveMenu,
            GameState.SettingsMenu,
            GameState.WarLedger,
            GameState.LevelUpScreen,
        };

        // Overlay states whose UI prefab already lives in the parent scene
        // (e.g. WarLedger is instantiated into BattleMap.unity by CursorSceneSetup
        // and driven via GameStateEventChannel). For these, SceneLoader must NOT
        // destroy the base scene and must NOT hunt Resources/Overlays/ for a prefab.
        private static readonly HashSet<GameState> InSceneOverlays = new()
        {
            GameState.WarLedger,
            GameState.LevelUpScreen,
        };

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
                DontDestroyOnLoad(eventSystem.gameObject);
        }

        private void Start()
        {
            _stateChangedChannel.Register(OnStateChanged);

            var initialState = GameStateManager.Instance.CurrentState;
            if (!IsOverlayState(initialState))
            {
                _currentBaseScene = initialState.ToString();
                SceneManager.LoadScene(_currentBaseScene);
            }
        }

        private void OnDestroy()
        {
            _stateChangedChannel.Unregister(OnStateChanged);
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (IsOverlayState(args.PreviousState))
                DestroyActiveOverlay();

            if (IsOverlayState(args.NewState))
            {
                // In-scene overlays (e.g. WarLedger) live inside the base scene already
                // and handle their own Show/Hide off GameStateEventChannel. Nothing to
                // instantiate here — silently skip the Resources lookup.
                if (InSceneOverlays.Contains(args.NewState)) return;

                var prefab = Resources.Load<GameObject>($"Overlays/{args.NewState}");
                if (prefab != null)
                    _activeOverlay = Instantiate(prefab);
                else
                    Debug.LogError($"[SceneLoader] Overlay prefab not found: Resources/Overlays/{args.NewState}");
            }
            else
            {
                DestroyActiveOverlay();

                string sceneName = args.NewState.ToString();
                if (sceneName != _currentBaseScene)
                {
                    _currentBaseScene = sceneName;
                    SceneManager.LoadScene(sceneName);
                }
            }
        }

        private void DestroyActiveOverlay()
        {
            if (_activeOverlay != null)
            {
                Destroy(_activeOverlay);
                _activeOverlay = null;
            }
        }

        private static bool IsOverlayState(GameState state) => OverlayStates.Contains(state);
    }
}
