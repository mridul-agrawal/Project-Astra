using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Cutscene UI — buttons to proceed to pre-battle prep or battle.
    /// </summary>
    public class CutsceneUI : MonoBehaviour
    {
        [SerializeField] private Button _preBattlePrepButton;
        [SerializeField] private Button _battleMapButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _buttons = new[] { _preBattlePrepButton, _battleMapButton };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void AddListenersToMouseClicks()
        {
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
            _preBattlePrepButton.onClick.RemoveListener(GoToPreBattlePrep);
            _battleMapButton.onClick.RemoveListener(GoToBattleMap);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
        }

        private void InitializeButtonColors() { foreach (var button in _buttons) button.image.color = Normal; }

        private void GoToPreBattlePrep() => GameStateManager.Instance.RequestTransition(GameState.PreBattlePrep, nameof(CutsceneUI));
        private void GoToBattleMap() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(CutsceneUI));

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
