using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Battle map debug UI — phase controls, allies toggle, and transition
    // buttons. Press Tab to toggle keyboard focus between this panel and the
    // grid cursor.
    public class BattleMapUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [Header("Phase Controls")]
        [SerializeField] private TextMeshProUGUI phaseLabel;
        [SerializeField] private Button advancePhaseButton;
        [SerializeField] private Toggle hasAlliesToggle;

        [Header("Transition Buttons")]
        [SerializeField] private Button cutsceneButton;
        [SerializeField] private Button combatAnimationButton;
        [SerializeField] private Button dialogueButton;
        [SerializeField] private Button chapterClearButton;
        [SerializeField] private Button gameOverButton;

        [Header("Colors")]
        [SerializeField] private Color Normal = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private BattlePhaseManager phaseManager;
        private bool hasAllies = true;
        private Button[] buttons;
        private int selected;

        // The four loose Canvas children that together make up the left-side
        // debug nav panel. Cached in Awake so we can toggle visibility atomically
        // when the user presses Tab.
        private GameObject[] navPanelRoots;

        private void Awake()
        {
            CacheNavPanelRoots();
            HasInputFocus = false;
            SetNavPanelVisible(false);
        }

        private void OnEnable()
        {
            phaseManager = new BattlePhaseManager(hasAllies);
            UpdatePhaseLabel();

            buttons = new[]
            {
                advancePhaseButton,
                cutsceneButton, combatAnimationButton, dialogueButton,
                chapterClearButton, gameOverButton
            };

            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            InitializeButtonColors();
            SelectButtonByIndex(0);

            hasAlliesToggle.isOn = hasAllies;
            hasAlliesToggle.onValueChanged.AddListener(OnHasAlliesChanged);
        }

        private void AddListenersToMouseClicks()
        {
            advancePhaseButton.onClick.AddListener(AdvancePhase);
            cutsceneButton.onClick.AddListener(GoToCutscene);
            combatAnimationButton.onClick.AddListener(GoToCombatAnimation);
            dialogueButton.onClick.AddListener(GoToDialogue);
            chapterClearButton.onClick.AddListener(GoToChapterClear);
            gameOverButton.onClick.AddListener(GoToGameOver);
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
            hasAlliesToggle.onValueChanged.RemoveListener(OnHasAlliesChanged);
        }

        private void RemoveListenersToMouseClicks()
        {
            advancePhaseButton.onClick.RemoveListener(AdvancePhase);
            cutsceneButton.onClick.RemoveListener(GoToCutscene);
            combatAnimationButton.onClick.RemoveListener(GoToCombatAnimation);
            dialogueButton.onClick.RemoveListener(GoToDialogue);
            chapterClearButton.onClick.RemoveListener(GoToChapterClear);
            gameOverButton.onClick.RemoveListener(GoToGameOver);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnCursorMove -= Navigate;
            InputManager.Instance.OnConfirm -= ConfirmSelection;
            InputManager.Instance.OnPause -= Pause;
        }

        private void InitializeButtonColors() { foreach (var button in buttons) button.image.color = Normal; }

        private void AdvancePhase()
        {
            phaseManager.AdvancePhase();
            UpdatePhaseLabel();
        }

        private void OnHasAlliesChanged(bool value)
        {
            hasAllies = value;
            phaseManager.SetHasAllies(value);
        }

        private void UpdatePhaseLabel()
        {
            phaseLabel.text = $"Phase: {phaseManager.CurrentPhase}";
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HasInputFocus = !HasInputFocus;
                SetNavPanelVisible(HasInputFocus);
            }
        }

        private void CacheNavPanelRoots()
        {
            navPanelRoots = new[]
            {
                transform.Find("Background")?.gameObject,
                transform.Find("Title")?.gameObject,
                transform.Find("NavigateLabel")?.gameObject,
                transform.Find("ButtonContainer")?.gameObject,
            };
        }

        private void SetNavPanelVisible(bool visible)
        {
            if (navPanelRoots == null) return;
            foreach (var go in navPanelRoots)
                if (go != null) go.SetActive(visible);
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
            if (IsNotActiveState || !HasInputFocus) return;
            if (dir.y > 0) SelectButtonByIndex(selected <= 0 ? buttons.Length - 1 : selected - 1);
            else if (dir.y < 0) SelectButtonByIndex(selected >= buttons.Length - 1 ? 0 : selected + 1);
        }

        private void ConfirmSelection()
        {
            if (IsNotActiveState || !HasInputFocus) return;
            buttons[selected].onClick.Invoke();
        }

        private void SelectButtonByIndex(int i)
        {
            buttons[selected].image.color = Normal;
            selected = i;
            buttons[selected].image.color = Selected;
        }
    }
}
