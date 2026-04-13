using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Game Over controller. Lives on the "GameOver" root built by GameOverBuilder.
    /// Discovers its buttons at OnEnable from ButtonsContainer's children in sibling order —
    /// index 0 → MainMenu, 1 → SaveMenu. The order must match GameOverBuilder.ButtonLabels.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private Color _normalTint   = new(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _selectedTint = new(1f, 1f, 1f, 1f);

        private Button[] _buttons;
        private int _selectedIndex;

        private void OnEnable()
        {
            if (!TryDiscoverButtons()) return;

            WireClicks();
            WireInput();
            InitializeButtonColors();
            SelectButtonByIndex(0);
        }

        private void OnDisable()
        {
            if (_buttons == null) return;
            UnwireClicks();
            UnwireInput();
        }

        private bool TryDiscoverButtons()
        {
            var container = transform.Find("ButtonsContainer");
            if (container == null)
            {
                Debug.LogError("[GameOverUI] ButtonsContainer child not found. " +
                               "Expected hierarchy: GameOver/ButtonsContainer/Button_NN_*. Did GameOverBuilder run?");
                return false;
            }

            var list = new List<Button>();
            foreach (Transform child in container)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) list.Add(btn);
            }

            if (list.Count != 2)
            {
                Debug.LogError($"[GameOverUI] Expected 2 buttons under ButtonsContainer, found {list.Count}. " +
                               "Check GameOverBuilder.ButtonLabels.");
                return false;
            }

            _buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            _buttons[0].onClick.AddListener(GoToMainMenu);
            _buttons[1].onClick.AddListener(GoToSaveMenu);
        }

        private void UnwireClicks()
        {
            _buttons[0].onClick.RemoveListener(GoToMainMenu);
            _buttons[1].onClick.RemoveListener(GoToSaveMenu);
        }

        private void WireInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm    += ConfirmSelection;
        }

        private void UnwireInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm    -= ConfirmSelection;
        }

        private void GoToMainMenu() => GameStateManager.Instance.RequestTransition(GameState.MainMenu, nameof(GameOverUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(GameOverUI));

        // Guards input callbacks against firing during an in-progress transition away from this screen.
        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.GameOver;

        private void Navigate(Vector2Int dir)
        {
            if (IsNotActiveState) return;
            if (dir.y > 0)      SelectButtonByIndex(_selectedIndex <= 0 ? _buttons.Length - 1 : _selectedIndex - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selectedIndex >= _buttons.Length - 1 ? 0 : _selectedIndex + 1);
        }

        private void ConfirmSelection()
        {
            if (IsNotActiveState) return;
            _buttons[_selectedIndex].onClick.Invoke();
        }

        private void InitializeButtonColors()
        {
            foreach (var b in _buttons)
                if (b.image != null) b.image.color = _normalTint;
        }

        private void SelectButtonByIndex(int i)
        {
            if (_buttons[_selectedIndex].image != null) _buttons[_selectedIndex].image.color = _normalTint;
            _selectedIndex = i;
            if (_buttons[_selectedIndex].image != null) _buttons[_selectedIndex].image.color = _selectedTint;
        }
    }
}
