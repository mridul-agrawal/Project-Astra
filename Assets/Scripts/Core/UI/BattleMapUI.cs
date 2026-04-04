using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Battle map UI — phase controls, allies toggle, and transition buttons. Subscribes to Pause.
    /// </summary>
    public class BattleMapUI : MonoBehaviour
    {
        [Header("Phase Controls")]
        [SerializeField] private TextMeshProUGUI _phaseLabel;
        [SerializeField] private Button _advancePhaseButton;
        [SerializeField] private Toggle _hasAlliesToggle;

        [Header("Transition Buttons")]
        [SerializeField] private Button _cutsceneButton;
        [SerializeField] private Button _combatAnimationButton;
        [SerializeField] private Button _dialogueButton;
        [SerializeField] private Button _chapterClearButton;
        [SerializeField] private Button _gameOverButton;

        [Header("Colors")]
        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private BattlePhaseManager _phaseManager;
        private bool _hasAllies = true;
        private Button[] _buttons;
        private int _selected;

        private void OnEnable()
        {
            _phaseManager = new BattlePhaseManager(_hasAllies);
            UpdatePhaseLabel();

            _buttons = new[]
            {
                _advancePhaseButton,
                _cutsceneButton, _combatAnimationButton, _dialogueButton,
                _chapterClearButton, _gameOverButton
            };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);

            _hasAlliesToggle.isOn = _hasAllies;
            _hasAlliesToggle.onValueChanged.AddListener(OnHasAlliesChanged);
        }

        private void AddListenersToMouseClicks()
        {
            _advancePhaseButton.onClick.AddListener(AdvancePhase);
            _cutsceneButton.onClick.AddListener(GoToCutscene);
            _combatAnimationButton.onClick.AddListener(GoToCombatAnimation);
            _dialogueButton.onClick.AddListener(GoToDialogue);
            _chapterClearButton.onClick.AddListener(GoToChapterClear);
            _gameOverButton.onClick.AddListener(GoToGameOver);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnPause += Pause;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
            _hasAlliesToggle.onValueChanged.RemoveListener(OnHasAlliesChanged);
        }

        private void RemoveListenersToMouseClicks()
        {
            _advancePhaseButton.onClick.RemoveListener(AdvancePhase);
            _cutsceneButton.onClick.RemoveListener(GoToCutscene);
            _combatAnimationButton.onClick.RemoveListener(GoToCombatAnimation);
            _dialogueButton.onClick.RemoveListener(GoToDialogue);
            _chapterClearButton.onClick.RemoveListener(GoToChapterClear);
            _gameOverButton.onClick.RemoveListener(GoToGameOver);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnPause -= Pause;
        }

        private void InitializeButtonColors() { foreach (var button in _buttons) button.image.color = Normal; }

        private void AdvancePhase()
        {
            _phaseManager.AdvancePhase();
            UpdatePhaseLabel();
        }

        private void OnHasAlliesChanged(bool value)
        {
            _hasAllies = value;
            _phaseManager.SetHasAllies(value);
        }

        private void UpdatePhaseLabel()
        {
            _phaseLabel.text = $"Phase: {_phaseManager.CurrentPhase}";
        }

        private void Pause()
        {
            if (IsNotActiveState) return;
            GameStateManager.Instance.RequestTransition(GameState.BattleMapPaused, nameof(BattleMapUI));
        }
        private void GoToCutscene() => GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(BattleMapUI));
        private void GoToCombatAnimation() => GameStateManager.Instance.RequestTransition(GameState.CombatAnimation, nameof(BattleMapUI));
        private void GoToDialogue() => GameStateManager.Instance.RequestTransition(GameState.Dialogue, nameof(BattleMapUI));
        private void GoToChapterClear() => GameStateManager.Instance.RequestTransition(GameState.ChapterClear, nameof(BattleMapUI));
        private void GoToGameOver() => GameStateManager.Instance.RequestTransition(GameState.GameOver, nameof(BattleMapUI));

        private bool IsNotActiveState => GameStateManager.Instance.CurrentState != GameState.BattleMap;

        private void Navigate(Vector2Int dir)
        {
            if (IsNotActiveState) return;
            if (dir.y > 0) SelectButtonByIndex(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection()
        {
            if (IsNotActiveState) return;
            _buttons[_selected].onClick.Invoke();
        }

        private void SelectButtonByIndex(int i)
        {
            _buttons[_selected].image.color = Normal;
            _selected = i;
            _buttons[_selected].image.color = Selected;
        }
    }
}
