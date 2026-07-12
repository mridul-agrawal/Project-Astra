using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Progression
{
    // Game Over controller. Lives on the "GameOver" root built by
    // GameOverBuilder. Discovers buttons at OnEnable from ButtonsContainer's
    // children in sibling order — index 0 → Title, 1 → SaveMenu. Order
    // must match GameOverBuilder.ButtonLabels.
    public class GameOverUI : MonoBehaviour
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
            AudioManager.Instance?.PlayMusic(SoundId.MusicGameOver);
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

            buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            buttons[0].onClick.AddListener(GoToTitle);
            buttons[1].onClick.AddListener(GoToSaveMenu);
        }

        private void UnwireClicks()
        {
            buttons[0].onClick.RemoveListener(GoToTitle);
            buttons[1].onClick.RemoveListener(GoToSaveMenu);
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

        private void GoToTitle() => GameStateManager.Instance.RequestTransition(GameState.TitleScreen, nameof(GameOverUI));
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(GameOverUI));

        // Guards input callbacks against firing during an in-progress transition away from this screen.
        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.GameOver;

        private void Navigate(Vector2Int dir)
        {
            if (IsNotActiveState) return;
            if (dir.y > 0)      SelectButtonByIndex(selectedIndex <= 0 ? buttons.Length - 1 : selectedIndex - 1);
            else if (dir.y < 0) SelectButtonByIndex(selectedIndex >= buttons.Length - 1 ? 0 : selectedIndex + 1);
            else return;
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void ConfirmSelection()
        {
            if (IsNotActiveState) return;
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
