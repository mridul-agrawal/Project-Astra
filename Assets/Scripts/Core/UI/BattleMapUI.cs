using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>Battle map UI — phase controls, allies toggle, and transition buttons. Subscribes to Pause.</summary>
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

        private static readonly Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

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

            _advancePhaseButton.onClick.AddListener(AdvancePhase);
            _cutsceneButton.onClick.AddListener(GoToCutscene);
            _combatAnimationButton.onClick.AddListener(GoToCombatAnimation);
            _dialogueButton.onClick.AddListener(GoToDialogue);
            _chapterClearButton.onClick.AddListener(GoToChapterClear);
            _gameOverButton.onClick.AddListener(GoToGameOver);

            _hasAlliesToggle.isOn = _hasAllies;
            _hasAlliesToggle.onValueChanged.AddListener(OnHasAlliesChanged);

            InputManager.Instance.OnCursorMove += Navigate;
            InputManager.Instance.OnConfirm += ConfirmSelection;
            InputManager.Instance.OnPause += Pause;

            Select(0);
        }

        private void OnDisable()
        {
            _advancePhaseButton.onClick.RemoveListener(AdvancePhase);
            _cutsceneButton.onClick.RemoveListener(GoToCutscene);
            _combatAnimationButton.onClick.RemoveListener(GoToCombatAnimation);
            _dialogueButton.onClick.RemoveListener(GoToDialogue);
            _chapterClearButton.onClick.RemoveListener(GoToChapterClear);
            _gameOverButton.onClick.RemoveListener(GoToGameOver);
            _hasAlliesToggle.onValueChanged.RemoveListener(OnHasAlliesChanged);

            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnPause -= Pause;
        }

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

        private void Pause() => GameStateManager.Instance.RequestTransition(GameState.BattleMapPaused, nameof(BattleMapUI));
        private void GoToCutscene() => GameStateManager.Instance.RequestTransition(GameState.Cutscene, nameof(BattleMapUI));
        private void GoToCombatAnimation() => GameStateManager.Instance.RequestTransition(GameState.CombatAnimation, nameof(BattleMapUI));
        private void GoToDialogue() => GameStateManager.Instance.RequestTransition(GameState.Dialogue, nameof(BattleMapUI));
        private void GoToChapterClear() => GameStateManager.Instance.RequestTransition(GameState.ChapterClear, nameof(BattleMapUI));
        private void GoToGameOver() => GameStateManager.Instance.RequestTransition(GameState.GameOver, nameof(BattleMapUI));

        private void Navigate(Vector2Int dir)
        {
            if (dir.y > 0) Select(_selected <= 0 ? _buttons.Length - 1 : _selected - 1);
            else if (dir.y < 0) Select(_selected >= _buttons.Length - 1 ? 0 : _selected + 1);
        }

        private void ConfirmSelection() => _buttons[_selected].onClick.Invoke();

        private void Select(int i)
        {
            _buttons[_selected].image.color = Normal;
            _selected = i;
            _buttons[_selected].image.color = Selected;
        }
    }
}
