using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Pause overlay — resume, save, or settings. Cancel returns to battle.</summary>
    public class BattleMapPausedOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveMenuButton;
        [SerializeField] private Button _settingsMenuButton;

        private static readonly Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _resumeButton, _saveMenuButton, _settingsMenuButton };

            _resumeButton.onClick.AddListener(Resume);
            _saveMenuButton.onClick.AddListener(GoToSaveMenu);
            _settingsMenuButton.onClick.AddListener(GoToSettingsMenu);

            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnCancel += Resume;

            Select(0);
        }

        private void OnDisable()
        {
            _resumeButton.onClick.RemoveListener(Resume);
            _saveMenuButton.onClick.RemoveListener(GoToSaveMenu);
            _settingsMenuButton.onClick.RemoveListener(GoToSettingsMenu);

            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnCancel -= Resume;
        }

        private void Resume() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(BattleMapPausedOverlayUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(BattleMapPausedOverlayUI));
        private void GoToSettingsMenu() => GameStateManager.Instance.RequestTransition(GameState.SettingsMenu, nameof(BattleMapPausedOverlayUI));

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0) Select(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) Select(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection() => _buttons[_selected].onClick.Invoke();

        private void Select(int i)
        {
            _buttons[_selected].image.color = Normal;
            _selected = i;
            _buttons[_selected].image.color = Selected;
        }
    }
}
