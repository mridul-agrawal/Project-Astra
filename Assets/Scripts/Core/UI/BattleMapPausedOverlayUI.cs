using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Pause overlay — resume, save, or settings. Cancel returns to battle.
    /// </summary>
    public class BattleMapPausedOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveMenuButton;
        [SerializeField] private Button _settingsMenuButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _resumeButton, _saveMenuButton, _settingsMenuButton };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void AddListenersToMouseClicks()
        {
            _resumeButton.onClick.AddListener(Resume);
            _saveMenuButton.onClick.AddListener(GoToSaveMenu);
            _settingsMenuButton.onClick.AddListener(GoToSettingsMenu);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnCancel += Resume;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            _resumeButton.onClick.RemoveListener(Resume);
            _saveMenuButton.onClick.RemoveListener(GoToSaveMenu);
            _settingsMenuButton.onClick.RemoveListener(GoToSettingsMenu);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnCancel -= Resume;
        }

        private void InitializeButtonColors() { foreach (var button in _buttons) button.image.color = Normal; }

        private void Resume() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(BattleMapPausedOverlayUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(BattleMapPausedOverlayUI));
        private void GoToSettingsMenu() => GameStateManager.Instance.RequestTransition(GameState.SettingsMenu, nameof(BattleMapPausedOverlayUI));

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0) SelectButtonByIndex(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection() => _buttons[_selected].onClick.Invoke();

        private void SelectButtonByIndex(int i)
        {
            _buttons[_selected].image.color = Normal;
            _selected = i;
            _buttons[_selected].image.color = Selected;
        }
    }
}
