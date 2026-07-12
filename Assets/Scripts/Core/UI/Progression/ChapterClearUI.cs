using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Progression
{
    // Chapter Clear controller. Lives on the "ChapterClear" root built by
    // ChapterClearBuilder. Discovers buttons at OnEnable from
    // ButtonsContainer's children in sibling order — index 0 → Cutscene,
    // 1 → SaveMenu. Order must match ChapterClearBuilder.ButtonLabels.
    public class ChapterClearUI : MonoBehaviour
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
            AudioManager.Instance?.PlayMusic(SoundId.MusicVictory);
            AudioManager.Instance?.Play(SoundId.Fanfare);
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

            buttons = list.ToArray();
            return true;
        }

        private void WireClicks()
        {
            buttons[0].onClick.AddListener(GoToCutscene);
            buttons[1].onClick.AddListener(GoToSaveMenu);
        }

        private void UnwireClicks()
        {
            buttons[0].onClick.RemoveListener(GoToCutscene);
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

        // The campaign decides what follows a cleared chapter; direct transition is the
        // editor direct-play fallback (no GameFlow when the scene runs standalone).
        private void GoToCutscene()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.NotifyBattleFinished();
            else GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(ChapterClearUI));
        }
        private void GoToSaveMenu() => GameStateManager.Instance.RequestTransition(GameState.SaveMenu, nameof(ChapterClearUI));

        // Guards input callbacks against firing during an in-progress transition away from this screen.
        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.ChapterClear;

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
