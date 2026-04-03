using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
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
            StartCoroutine(PopulateUI());
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            StartCoroutine(PopulateUI());
        }

        // Wait one frame for scene/overlay to finish loading
        private IEnumerator PopulateUI()
        {
            yield return null;

            var root = FindFirstObjectByType<SceneUIRoot>();
            if (root == null || root.ButtonContainer == null) yield break;

            ClearContainer(root.ButtonContainer);

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
        }

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
            if (_buttonPrefab != null)
            {
                var instance = Instantiate(_buttonPrefab, container);
                instance.name = label;
                var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;
                var button = instance.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(onClick);
                return;
            }

            // Fallback: create button from code if prefab missing
            var buttonGo = new GameObject(label, typeof(RectTransform));
            buttonGo.transform.SetParent(container, false);
            buttonGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            var img = buttonGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            var btn = buttonGo.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
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
