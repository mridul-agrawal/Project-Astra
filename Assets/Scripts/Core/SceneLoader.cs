using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ProjectAstra.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        [Header("Overlay Prefabs")]
        [SerializeField] private GameObject _battleMapPausedOverlay;
        [SerializeField] private GameObject _combatAnimationOverlay;
        [SerializeField] private GameObject _dialogueOverlay;
        [SerializeField] private GameObject _saveMenuOverlay;
        [SerializeField] private GameObject _settingsMenuOverlay;

        private string _currentBaseScene;
        private GameObject _activeOverlay;

        private static readonly HashSet<GameState> OverlayStates = new()
        {
            GameState.BattleMapPaused,
            GameState.CombatAnimation,
            GameState.Dialogue,
            GameState.SaveMenu,
            GameState.SettingsMenu
        };

        private static readonly Dictionary<GameState, string> SceneNames = new()
        {
            { GameState.TitleScreen,   "TitleScreen" },
            { GameState.MainMenu,      "MainMenu" },
            { GameState.Cutscene,      "Cutscene" },
            { GameState.PreBattlePrep, "PreBattlePrep" },
            { GameState.BattleMap,     "BattleMap" },
            { GameState.ChapterClear,  "ChapterClear" },
            { GameState.GameOver,      "GameOver" },
        };

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // EventSystem must survive scene loads or UI input stops working
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
                DontDestroyOnLoad(eventSystem.gameObject);
        }

        private void Start()
        {
            _stateChangedChannel.Register(OnStateChanged);

            var initialState = GameStateManager.Instance.CurrentState;
            if (SceneNames.TryGetValue(initialState, out string sceneName))
            {
                _currentBaseScene = sceneName;
                SceneManager.LoadScene(sceneName);
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
                InstantiateOverlay(args.NewState);
            }
            else
            {
                DestroyActiveOverlay();

                if (SceneNames.TryGetValue(args.NewState, out string sceneName)
                    && sceneName != _currentBaseScene)
                {
                    _currentBaseScene = sceneName;
                    SceneManager.LoadScene(sceneName);
                }
            }
        }

        private void InstantiateOverlay(GameState state)
        {
            var prefab = GetOverlayPrefab(state);
            if (prefab != null)
                _activeOverlay = Instantiate(prefab);
        }

        private void DestroyActiveOverlay()
        {
            if (_activeOverlay != null)
            {
                Destroy(_activeOverlay);
                _activeOverlay = null;
            }
        }

        private GameObject GetOverlayPrefab(GameState state)
        {
            return state switch
            {
                GameState.BattleMapPaused  => _battleMapPausedOverlay,
                GameState.CombatAnimation  => _combatAnimationOverlay,
                GameState.Dialogue         => _dialogueOverlay,
                GameState.SaveMenu         => _saveMenuOverlay,
                GameState.SettingsMenu     => _settingsMenuOverlay,
                _ => null
            };
        }

        private static bool IsOverlayState(GameState state) => OverlayStates.Contains(state);
    }
}
