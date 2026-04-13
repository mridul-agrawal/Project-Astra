using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Chapter Clear controller. Lives on the "ChapterClear" root built by ChapterClearBuilder.
    /// Discovers its buttons at OnEnable from ButtonsContainer's children in sibling order —
    /// index 0 → Cutscene, 1 → SaveMenu. The order must match ChapterClearBuilder.ButtonLabels.
    /// </summary>
    public class ChapterClearUI : MonoBehaviour
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
                Debug.LogError("[ChapterClearUI] ButtonsContainer child not found. " +
                               "Expected hierarchy: ChapterClear/ButtonsContainer/Button_NN_*. Did ChapterClearBuilder run?");
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
                Debug.LogError($"[ChapterClearUI] Expected 2 buttons under ButtonsContainer, found {list.Count}. " +
                               "Check ChapterClearBuilder.ButtonLabels.");
                return false;
            }

            _buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            _buttons[0].onClick.AddListener(GoToCutscene);
            _buttons[1].onClick.AddListener(GoToSaveMenu);
        }

        private void UnwireClicks()
        {
            _buttons[0].onClick.RemoveListener(GoToCutscene);
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

        private void GoToCutscene() => GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(ChapterClearUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(ChapterClearUI));

        // Guards input callbacks against firing during an in-progress transition away from this screen.
        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.ChapterClear;

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
