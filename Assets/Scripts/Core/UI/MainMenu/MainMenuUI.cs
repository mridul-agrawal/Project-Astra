using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.MainMenu
{
    // Main menu controller. Lives on the "MainMenu" root built by
    // MainMenuBuilder. Discovers its buttons at OnEnable from
    // ButtonsContainer's children in sibling order — index 0 → Cutscene,
    // 1 → PreBattlePrep, 2 → BattleMap. Order must match
    // MainMenuBuilder.ButtonLabels.
    public class MainMenuUI : MonoBehaviour
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
                Debug.LogError("[MainMenuUI] ButtonsContainer child not found. " +
                               "Expected hierarchy: MainMenu/ButtonsContainer/Button_NN_*. Did MainMenuBuilder run?");
                return false;
            }

            var list = new List<Button>();
            foreach (Transform child in container)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) list.Add(btn);
            }

            if (list.Count != 3)
            {
                Debug.LogError($"[MainMenuUI] Expected 3 buttons under ButtonsContainer, found {list.Count}. " +
                               "Check MainMenuBuilder.ButtonLabels.");
                return false;
            }

            buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            buttons[0].onClick.AddListener(GoToCutscene);
            buttons[1].onClick.AddListener(GoToPreBattlePrep);
            buttons[2].onClick.AddListener(GoToBattleMap);
        }

        private void UnwireClicks()
        {
            buttons[0].onClick.RemoveListener(GoToCutscene);
            buttons[1].onClick.RemoveListener(GoToPreBattlePrep);
            buttons[2].onClick.RemoveListener(GoToBattleMap);
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

        private void GoToCutscene()      => GameFlow.Instance.Begin();   // "New Game" → start the campaign at its first beat
        private void GoToPreBattlePrep() => GameStateManager.Instance.RequestTransition(GameState.PreBattlePrep, nameof(MainMenuUI));
        private void GoToBattleMap()     => GameStateManager.Instance.RequestTransition(GameState.BattleMap,     nameof(MainMenuUI));

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0)      SelectButtonByIndex(selectedIndex <= 0 ? buttons.Length - 1 : selectedIndex - 1);
            else if (dir.y < 0) SelectButtonByIndex(selectedIndex >= buttons.Length - 1 ? 0 : selectedIndex + 1);
            else return;
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void ConfirmSelection()
        {
            AudioManager.Instance?.Play(SoundId.UiConfirm);
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
