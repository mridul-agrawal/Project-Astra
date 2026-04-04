using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Main menu UI — buttons for starting a new game, prep, or jumping to battle.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _cutsceneButton;
        [SerializeField] private Button _preBattlePrepButton;
        [SerializeField] private Button _battleMapButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _cutsceneButton, _preBattlePrepButton, _battleMapButton };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void AddListenersToMouseClicks()
        {
                _cutsceneButton.onClick.AddListener(GoToCutscene);
                _preBattlePrepButton.onClick.AddListener(GoToPreBattlePrep);
                _battleMapButton.onClick.AddListener(GoToBattleMap);
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
                _cutsceneButton.onClick.RemoveListener(GoToCutscene);
                _preBattlePrepButton.onClick.RemoveListener(GoToPreBattlePrep);
                _battleMapButton.onClick.RemoveListener(GoToBattleMap);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
        }

        private void InitializeButtonColors() { foreach (var button in _buttons) button.image.color = Normal; }

        private void GoToCutscene() => GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(MainMenuUI));
        private void GoToPreBattlePrep() => GameStateManager.Instance.RequestTransition(GameState.PreBattlePrep, nameof(MainMenuUI));
        private void GoToBattleMap() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(MainMenuUI));

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
