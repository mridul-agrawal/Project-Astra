using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core
{
    public class StateUIController : MonoBehaviour
    {
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private GameStateTransitionTable _transitionTable;

        private GameObject _buttonPrefab;
        private BattlePhaseManager _battlePhaseManager;
        private bool _hasAllies = true;
        private TextMeshProUGUI _phaseLabel;
        private readonly List<Button> _navigationButtons = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Cursor.visible = false;
            _buttonPrefab = Resources.Load<GameObject>("UI/NavigationButton");
        }

        private void OnEnable()
        {
            _stateChangedChannel.Register(OnStateChanged);
        }

        private void OnDisable()
        {
            _stateChangedChannel.Unregister(OnStateChanged);
        }

        private void Start()
        {
            SubscribeToInputActions();
            StartCoroutine(PopulateUI());
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            StartCoroutine(PopulateUI());
        }

        private IEnumerator PopulateUI()
        {
            yield return null;

            var root = FindFirstObjectByType<SceneUIRoot>();
            if (root == null || root.ButtonContainer == null) yield break;

            ClearContainer(root.ButtonContainer);
            _navigationButtons.Clear();

            var currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.BattleMap)
                SetupBattlePhaseControls(root.ButtonContainer);
            else
                _battlePhaseManager = null;

            if (currentState == GameState.SaveMenu || currentState == GameState.SettingsMenu)
                CreateButton(root.ButtonContainer, "Return", () =>
                    GameStateManager.Instance.ReturnFromContextMenu("SceneUI"));
            else
                CreateTransitionButtons(root.ButtonContainer, currentState);

            SetupButtonNavigation();
        }

        private void SetupButtonNavigation()
        {
            if (_navigationButtons.Count == 0) return;

            for (int i = 0; i < _navigationButtons.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = _navigationButtons[i > 0 ? i - 1 : _navigationButtons.Count - 1];
                nav.selectOnDown = _navigationButtons[i < _navigationButtons.Count - 1 ? i + 1 : 0];
                _navigationButtons[i].navigation = nav;
            }

            // Auto-select the first button for keyboard navigation
            EventSystem.current?.SetSelectedGameObject(_navigationButtons[0].gameObject);
        }

        #region Contextual Input Actions

        private void SubscribeToInputActions()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnPause += HandlePause;
            InputManager.Instance.OnCancel += HandleCancel;
        }

        private void HandlePause()
        {
            var state = GameStateManager.Instance.CurrentState;
            if (state == GameState.BattleMap)
                GameStateManager.Instance.RequestTransition(GameState.BattleMapPaused, "InputAction");
        }

        private void HandleCancel()
        {
            var state = GameStateManager.Instance.CurrentState;

            if (state == GameState.BattleMapPaused)
                GameStateManager.Instance.RequestTransition(GameState.BattleMap, "InputAction");
            else if (state == GameState.SaveMenu || state == GameState.SettingsMenu)
                GameStateManager.Instance.ReturnFromContextMenu("InputAction");
        }

        #endregion

        #region Button/UI Creation

        private void CreateTransitionButtons(Transform container, GameState currentState)
        {
            var allStates = (GameState[])Enum.GetValues(typeof(GameState));
            foreach (var target in allStates)
            {
                if (!_transitionTable.IsValid(currentState, target)) continue;
                var targetState = target;
                CreateButton(container, FormatName(target.ToString()), () =>
                    GameStateManager.Instance.RequestTransition(targetState, "SceneUI"));
            }
        }

        private void SetupBattlePhaseControls(Transform container)
        {
            _battlePhaseManager = new BattlePhaseManager(_hasAllies);

            _phaseLabel = CreateLabel(container,
                $"Battle Phase:  {FormatName(_battlePhaseManager.CurrentPhase.ToString())}");

            CreateButton(container, "Advance Phase", () =>
            {
                _battlePhaseManager.AdvancePhase();
                _phaseLabel.text = $"Battle Phase:  {FormatName(_battlePhaseManager.CurrentPhase.ToString())}";
            });

            CreateToggle(container, "Has Allies", _hasAllies, value =>
            {
                _hasAllies = value;
                _battlePhaseManager.SetHasAllies(value);
            });

            CreateSpacer(container, 20f);
        }

        private void CreateButton(Transform container, string label, UnityEngine.Events.UnityAction onClick)
        {
            Button button;

            if (_buttonPrefab != null)
            {
                var instance = Instantiate(_buttonPrefab, container);
                instance.name = label;
                var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;
                button = instance.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(onClick);
            }
            else
            {
                var buttonGo = new GameObject(label, typeof(RectTransform));
                buttonGo.transform.SetParent(container, false);
                buttonGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
                var img = buttonGo.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                button = buttonGo.AddComponent<Button>();
                button.onClick.AddListener(onClick);
                buttonGo.AddComponent<LayoutElement>().preferredHeight = 50;

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(buttonGo.transform, false);
                var text = textGo.AddComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 22;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            if (button != null)
                _navigationButtons.Add(button);
        }

        private TextMeshProUGUI CreateLabel(Transform container, string text)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(container, false);
            labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.85f, 0.85f);
            tmp.raycastTarget = false;
            labelGo.AddComponent<LayoutElement>().preferredHeight = 35;
            return tmp;
        }

        private void CreateToggle(Transform container, string label, bool initialValue, Action<bool> onValueChanged)
        {
            var toggleGo = new GameObject(label, typeof(RectTransform));
            toggleGo.transform.SetParent(container, false);
            toggleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            toggleGo.AddComponent<LayoutElement>().preferredHeight = 40;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = initialValue;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(toggleGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(30, 30);
            bgRect.anchoredPosition = new Vector2(10, 0);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = Color.white;
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(toggleGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.85f, 0.85f, 0.85f);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(50, 0);
            textRect.offsetMax = Vector2.zero;

            toggle.onValueChanged.AddListener(val => onValueChanged(val));
        }

        private void CreateSpacer(Transform container, float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(container, false);
            spacer.AddComponent<LayoutElement>().preferredHeight = height;
        }

        #endregion

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        private static string FormatName(string pascalCase)
        {
            return Regex.Replace(pascalCase, @"(?<!^)([A-Z])", " $1");
        }
    }
}
