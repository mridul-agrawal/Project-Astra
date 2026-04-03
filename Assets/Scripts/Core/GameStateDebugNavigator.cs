using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectAstra.Core
{
    public class GameStateDebugNavigator : MonoBehaviour
    {
        [SerializeField] private GameStateTransitionTable _transitionTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        private BattlePhaseManager _battlePhaseManager;
        private bool _hasAllies = true;
        private GameState[] _allStates;
        private Texture2D _backgroundTexture;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesInitialized;
        private Vector2 _scrollPosition;

        private static readonly Dictionary<GameState, Color> StateColors = new()
        {
            { GameState.TitleScreen,      new Color(0.10f, 0.12f, 0.30f) },
            { GameState.MainMenu,         new Color(0.25f, 0.27f, 0.30f) },
            { GameState.Cutscene,         new Color(0.22f, 0.10f, 0.30f) },
            { GameState.PreBattlePrep,    new Color(0.28f, 0.28f, 0.15f) },
            { GameState.BattleMap,        new Color(0.10f, 0.25f, 0.12f) },
            { GameState.BattleMapPaused,  new Color(0.18f, 0.28f, 0.20f) },
            { GameState.CombatAnimation,  new Color(0.35f, 0.10f, 0.10f) },
            { GameState.Dialogue,         new Color(0.10f, 0.25f, 0.28f) },
            { GameState.ChapterClear,     new Color(0.35f, 0.30f, 0.08f) },
            { GameState.GameOver,         new Color(0.30f, 0.08f, 0.08f) },
            { GameState.SaveMenu,         new Color(0.20f, 0.22f, 0.28f) },
            { GameState.SettingsMenu,     new Color(0.22f, 0.22f, 0.22f) },
        };

        private void Awake()
        {
            _allStates = (GameState[])Enum.GetValues(typeof(GameState));
            _backgroundTexture = new Texture2D(1, 1);
        }

        private void OnEnable()
        {
            _stateChangedChannel.Register(OnStateChanged);
        }

        private void OnDisable()
        {
            _stateChangedChannel.Unregister(OnStateChanged);
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null)
                Destroy(_backgroundTexture);
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap)
                _battlePhaseManager = new BattlePhaseManager(_hasAllies);
            else if (args.PreviousState == GameState.BattleMap)
                _battlePhaseManager = null;
        }

        private void InitStyles()
        {
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fixedHeight = 40,
                margin = new RectOffset(0, 0, 4, 4)
            };

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (GameStateManager.Instance == null) return;
            if (!_stylesInitialized) InitStyles();

            var currentState = GameStateManager.Instance.CurrentState;

            DrawBackground(currentState);

            float panelWidth = 400;
            float panelX = (Screen.width - panelWidth) / 2f;
            float y = 40;

            GUILayout.BeginArea(new Rect(panelX, y, panelWidth, Screen.height - 80));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label(FormatStateName(currentState), _titleStyle);
            GUILayout.Space(8);

            if (currentState == GameState.BattleMap && _battlePhaseManager != null)
                DrawBattlePhaseControls();

            if (currentState == GameState.SaveMenu || currentState == GameState.SettingsMenu)
                DrawContextMenuReturnButton();
            else
                DrawTransitionButtons(currentState);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawBackground(GameState state)
        {
            if (StateColors.TryGetValue(state, out var color))
            {
                _backgroundTexture.SetPixel(0, 0, color);
                _backgroundTexture.Apply();
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _backgroundTexture);
            }
        }

        private void DrawBattlePhaseControls()
        {
            GUILayout.Label($"Battle Phase:  {FormatPhaseName(_battlePhaseManager.CurrentPhase)}", _subtitleStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Advance Phase", _buttonStyle, GUILayout.Width(190)))
                _battlePhaseManager.AdvancePhase();

            bool newHasAllies = GUILayout.Toggle(_hasAllies, " Has Allies", GUILayout.Height(40));
            if (newHasAllies != _hasAllies)
            {
                _hasAllies = newHasAllies;
                _battlePhaseManager.SetHasAllies(_hasAllies);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
        }

        private void DrawTransitionButtons(GameState currentState)
        {
            GUILayout.Label("Navigate to:", _sectionStyle);
            GUILayout.Space(4);

            foreach (var target in _allStates)
            {
                if (!_transitionTable.IsValid(currentState, target)) continue;

                if (GUILayout.Button(FormatStateName(target), _buttonStyle))
                    GameStateManager.Instance.RequestTransition(target, "DebugNavigator");
            }
        }

        private void DrawContextMenuReturnButton()
        {
            GUILayout.Label("Navigate to:", _sectionStyle);
            GUILayout.Space(4);

            var returnState = GameStateManager.Instance.MenuReturnState;
            if (GUILayout.Button($"Return to {FormatStateName(returnState)}", _buttonStyle))
                GameStateManager.Instance.ReturnFromContextMenu("DebugNavigator");
        }

        // Inserts spaces before uppercase letters: "BattleMapPaused" → "Battle Map Paused"
        private static string FormatStateName(GameState state)
        {
            return Regex.Replace(state.ToString(), @"(?<!^)([A-Z])", " $1");
        }

        private static string FormatPhaseName(BattlePhase phase)
        {
            return Regex.Replace(phase.ToString(), @"(?<!^)([A-Z])", " $1");
        }
    }
}
