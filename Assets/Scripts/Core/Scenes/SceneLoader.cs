using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Scenes
{
    // Mediates between game state changes and Unity scene/overlay loading.
    public class SceneLoader : MonoBehaviour
    {
        const string OverlayResourceFolder = "Overlays";

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

        // Overlays whose UI prefab is already inside the parent scene (e.g. WarLedger is built into BattleMap.unity
        // by CursorSceneSetup and driven via GameStateEventChannel). For these we must NOT swap scenes or hunt for a prefab.
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
            DestroyActiveOverlay();

            if (IsOverlayState(args.NewState))
                ShowOverlayFor(args.NewState);
            else
                LoadBaseSceneFor(args.NewState);
        }

        private void ShowOverlayFor(GameState state)
        {
            // In-scene overlays handle their own Show/Hide via GameStateEventChannel; nothing to instantiate here.
            if (InSceneOverlays.Contains(state)) return;

            var prefab = Resources.Load<GameObject>($"{OverlayResourceFolder}/{state}");
            if (prefab != null)
                _activeOverlay = Instantiate(prefab);
            else
                Debug.LogError($"[SceneLoader] Overlay prefab not found: Resources/{OverlayResourceFolder}/{state}");
        }

        private void LoadBaseSceneFor(GameState state)
        {
            string sceneName = state.ToString();
            if (sceneName == _currentBaseScene) return;

            _currentBaseScene = sceneName;
            SceneManager.LoadScene(sceneName);
        }

        private void DestroyActiveOverlay()
        {
            if (_activeOverlay == null) return;
            Destroy(_activeOverlay);
            _activeOverlay = null;
        }

        private static bool IsOverlayState(GameState state) => OverlayStates.Contains(state);
    }
}
