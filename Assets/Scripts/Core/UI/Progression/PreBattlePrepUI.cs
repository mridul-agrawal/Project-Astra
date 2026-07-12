using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Progression
{
    // Pre Battle Prep controller. Lives on the "PreBattlePrep" root built by
    // PreBattlePrepBuilder. Discovers its single button at OnEnable from
    // ButtonsContainer's first child — index 0 → BattleMap. Order must match
    // PreBattlePrepBuilder.ButtonLabels.
    public class PreBattlePrepUI : MonoBehaviour
    {
        [SerializeField] private Color normalTint   = new(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color selectedTint = new(1f, 1f, 1f, 1f);

        private Button[] buttons;
        private int selectedIndex;

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
            if (buttons == null) return;
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

            buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            buttons[0].onClick.AddListener(GoToBattleMap);
        }

        private void UnwireClicks()
        {
            buttons[0].onClick.RemoveListener(GoToBattleMap);
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
            if (dir.y > 0)      SelectButtonByIndex(selectedIndex <= 0 ? buttons.Length - 1 : selectedIndex - 1);
            else if (dir.y < 0) SelectButtonByIndex(selectedIndex >= buttons.Length - 1 ? 0 : selectedIndex + 1);
        }

        private void ConfirmSelection()
        {
            AudioManager.Instance?.Play(SoundId.ConfirmStartBattle);
            buttons[selectedIndex].onClick.Invoke();
        }

        private void InitializeButtonColors()
        {
            foreach (var b in buttons)
                if (b.image != null) b.image.color = normalTint;
        }

        private void SelectButtonByIndex(int i)
        {
            if (buttons[selectedIndex].image != null) buttons[selectedIndex].image.color = normalTint;
            selectedIndex = i;
            if (buttons[selectedIndex].image != null) buttons[selectedIndex].image.color = selectedTint;
        }
    }
}
