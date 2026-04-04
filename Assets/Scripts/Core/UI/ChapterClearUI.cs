using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Chapter clear UI — buttons to continue to next cutscene or save.</summary>
    public class ChapterClearUI : MonoBehaviour
    {
        [SerializeField] private Button _cutsceneButton;
        [SerializeField] private Button _saveMenuButton;

        private static readonly Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _cutsceneButton, _saveMenuButton };

            _cutsceneButton.onClick.AddListener(GoToCutscene);
            _saveMenuButton.onClick.AddListener(GoToSaveMenu);

            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;

            Select(0);
        }

        private void OnDisable()
        {
            _cutsceneButton.onClick.RemoveListener(GoToCutscene);
            _saveMenuButton.onClick.RemoveListener(GoToSaveMenu);

            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
        }

        private void GoToCutscene() => GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(ChapterClearUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(ChapterClearUI));

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
