using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Pre Battle Prep controller. Lives on the "PreBattlePrep" root built by PreBattlePrepBuilder.
    /// Discovers its single button at OnEnable from ButtonsContainer's first child —
    /// index 0 → BattleMap. The order must match PreBattlePrepBuilder.ButtonLabels.
    /// </summary>
    public class PreBattlePrepUI : MonoBehaviour
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
                Debug.LogError("[PreBattlePrepUI] ButtonsContainer child not found. " +
                               "Expected hierarchy: PreBattlePrep/ButtonsContainer/Button_NN_*. Did PreBattlePrepBuilder run?");
                return false;
            }

            var list = new List<Button>();
            foreach (Transform child in container)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) list.Add(btn);
            }

            if (list.Count != 1)
            {
                Debug.LogError($"[PreBattlePrepUI] Expected 1 button under ButtonsContainer, found {list.Count}. " +
                               "Check PreBattlePrepBuilder.ButtonLabels.");
                return false;
            }

            _buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            _buttons[0].onClick.AddListener(GoToBattleMap);
        }

        private void UnwireClicks()
        {
            _buttons[0].onClick.RemoveListener(GoToBattleMap);
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

        private void GoToBattleMap() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(PreBattlePrepUI));

        private void Navigate(Vector2Int dir)
        {
            // With a single button, up/down wrap back to the only index (0).
            if (dir.y > 0)      SelectButtonByIndex(_selectedIndex <= 0 ? _buttons.Length - 1 : _selectedIndex - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selectedIndex >= _buttons.Length - 1 ? 0 : _selectedIndex + 1);
        }

        private void ConfirmSelection() => _buttons[_selectedIndex].onClick.Invoke();

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
