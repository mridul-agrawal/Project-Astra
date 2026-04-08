using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    public class BattleMapPausedOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveMenuButton;
        [SerializeField] private Button _settingsMenuButton;
        [SerializeField] private Button _quitButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;
        private bool _confirmingQuit;

        private void OnEnable()
        {
            _buttons = new[] { _endTurnButton, _resumeButton, _saveMenuButton, _settingsMenuButton, _quitButton };
            _confirmingQuit = false;

            AddListeners();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        private void AddListeners()
        {
            if (_endTurnButton != null) _endTurnButton.onClick.AddListener(EndTurn);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_saveMenuButton != null) _saveMenuButton.onClick.AddListener(GoToSaveMenu);
            if (_settingsMenuButton != null) _settingsMenuButton.onClick.AddListener(GoToSettingsMenu);
            if (_quitButton != null) _quitButton.onClick.AddListener(ConfirmQuit);

            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnCancel += HandleCancel;
        }

        private void RemoveListeners()
        {
            if (_endTurnButton != null) _endTurnButton.onClick.RemoveListener(EndTurn);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Resume);
            if (_saveMenuButton != null) _saveMenuButton.onClick.RemoveListener(GoToSaveMenu);
            if (_settingsMenuButton != null) _settingsMenuButton.onClick.RemoveListener(GoToSettingsMenu);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(ConfirmQuit);

            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnCancel -= HandleCancel;
        }

        private void EndTurn()
        {
            TurnManager.Instance?.EndPlayerPhase();
            Resume();
        }

        private void Resume()
        {
            GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(BattleMapPausedOverlayUI));
        }

        private void GoToSaveMenu()
        {
            Debug.Log("Suspend save not yet implemented.");
            Resume();
        }

        private void GoToSettingsMenu()
        {
            GameStateManager.Instance.RequestTransition(GameState.SettingsMenu, nameof(BattleMapPausedOverlayUI));
        }

        private void ConfirmQuit()
        {
            GameStateManager.Instance.RequestTransition(GameState.MainMenu, nameof(BattleMapPausedOverlayUI));
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0) SelectButtonByIndex(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection()
        {
            _buttons[_selected].onClick.Invoke();
        }

        private void HandleCancel()
        {
            Resume();
        }

        private void InitializeButtonColors()
        {
            foreach (var button in _buttons)
                if (button != null)
                    button.image.color = Normal;
        }

        private void SelectButtonByIndex(int i)
        {
            if (_buttons[_selected] != null) _buttons[_selected].image.color = Normal;
            _selected = i;
            if (_buttons[_selected] != null) _buttons[_selected].image.color = Selected;
        }
    }
}
