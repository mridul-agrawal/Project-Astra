using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Game over UI — buttons to return to main menu or load a save.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _saveMenuButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _mainMenuButton, _saveMenuButton };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void AddListenersToMouseClicks()
        {
            _mainMenuButton.onClick.AddListener(GoToMainMenu);
            _saveMenuButton.onClick.AddListener(GoToSaveMenu);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            _mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            _saveMenuButton.onClick.RemoveListener(GoToSaveMenu);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
        }

        private void InitializeButtonColors() { foreach (var button in _buttons) button.image.color = Normal; }

        private void GoToMainMenu() => GameStateManager.Instance.RequestTransition(GameState.MainMenu, nameof(GameOverUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(GameOverUI));

        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.GameOver;

        private void Navigate(Vector2Int dir)
        {
            if (IsNotActiveState) return;
            if (dir.y > 0) SelectButtonByIndex(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection()
        {
            if (IsNotActiveState) return;
            _buttons[_selected].onClick.Invoke();
        }

        private void SelectButtonByIndex(int i)
        {
            _buttons[_selected].image.color = Normal;
            _selected = i;
            _buttons[_selected].image.color = Selected;
        }
    }
}
