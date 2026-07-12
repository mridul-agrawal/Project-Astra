using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.UI.Overlays
{
    // Pause overlay shown during BattleMap play. Lists End Turn / Resume /
    // Save / Settings / Quit; Cancel returns to BattleMap.
    public class BattleMapPausedOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveMenuButton;
        [SerializeField] private Button settingsMenuButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private Button[] buttons;
        private int selected;
        private bool confirmingQuit;

        private void OnEnable()
        {
            buttons = new[] { endTurnButton, resumeButton, saveMenuButton, settingsMenuButton, quitButton };
            confirmingQuit = false;

            AddListeners();
            InitializeButtonColors();
            SelectButtonByIndex(0);
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        private void AddListeners()
        {
            if (endTurnButton != null) endTurnButton.onClick.AddListener(EndTurn);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (saveMenuButton != null) saveMenuButton.onClick.AddListener(GoToSaveMenu);
            if (settingsMenuButton != null) settingsMenuButton.onClick.AddListener(GoToSettingsMenu);
            if (quitButton != null) quitButton.onClick.AddListener(ConfirmQuit);

            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnCancel += HandleCancel;
        }

        private void RemoveListeners()
        {
            if (endTurnButton != null) endTurnButton.onClick.RemoveListener(EndTurn);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (saveMenuButton != null) saveMenuButton.onClick.RemoveListener(GoToSaveMenu);
            if (settingsMenuButton != null) settingsMenuButton.onClick.RemoveListener(GoToSettingsMenu);
            if (quitButton != null) quitButton.onClick.RemoveListener(ConfirmQuit);

            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnCancel -= HandleCancel;
        }

        private void EndTurn()
        {
            TurnManager.Instance?.EndPlayerPhase();
            Resume();
        }

        private void Resume()
        {
            GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(BattleMapPausedOverlayUI));
        }

        private void GoToSaveMenu()
        {
            Debug.Log("Suspend save not yet implemented.");
            Resume();
        }

        private void GoToSettingsMenu()
        {
            GameStateManager.Instance.RequestTransition(GameState.SettingsMenu, nameof(BattleMapPausedOverlayUI));
        }

        private void ConfirmQuit()
        {
            GameStateManager.Instance.RequestTransition(GameState.TitleScreen, nameof(BattleMapPausedOverlayUI));
        }

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0) SelectButtonByIndex(selected <= 0 ? buttons.Length - 1 : selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(selected >= buttons.Length - 1 ? 0 : selected + 1);
            else return;
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void ConfirmSelection()
        {
            AudioManager.Instance?.Play(SoundId.UiConfirm);
            buttons[selected].onClick.Invoke();
        }

        private void HandleCancel()
        {
            AudioManager.Instance?.Play(SoundId.UiCancel);
            Resume();
        }

        private void InitializeButtonColors()
        {
            foreach (var button in buttons)
                if (button != null)
                    button.image.color = Normal;
        }

        private void SelectButtonByIndex(int i)
        {
            if (buttons[selected] != null) buttons[selected].image.color = Normal;
            selected = i;
            if (buttons[selected] != null) buttons[selected].image.color = Selected;
        }
    }
}
