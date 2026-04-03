using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core
{
    public class StateUIController : MonoBehaviour
    {
        [SerializeField] private GameState _state;
        [SerializeField] private GameStateTransitionTable _transitionTable;
        [SerializeField] private Transform _buttonContainer;

        private BattlePhaseManager _battlePhaseManager;
        private bool _hasAllies = true;

        // Cached references for battle phase UI (created at runtime)
        private TextMeshProUGUI _phaseLabel;

        private void Start()
        {
            if (_transitionTable == null)
            {
                Debug.LogError($"[StateUIController] TransitionTable not assigned on {gameObject.name} in scene for state {_state}");
                return;
            }

            if (_state == GameState.BattleMap)
                SetupBattlePhaseControls();

            if (_state == GameState.SaveMenu || _state == GameState.SettingsMenu)
                CreateReturnButton();
            else
                CreateTransitionButtons();
        }

        private void CreateTransitionButtons()
        {
            var allStates = (GameState[])Enum.GetValues(typeof(GameState));
            foreach (var target in allStates)
            {
                if (!_transitionTable.IsValid(_state, target)) continue;
                CreateButton(FormatName(target.ToString()), () =>
                    GameStateManager.Instance.RequestTransition(target, "SceneUI"));
            }
        }

        private void CreateReturnButton()
        {
            CreateButton("Return", () =>
                GameStateManager.Instance.ReturnFromContextMenu("SceneUI"));
        }

        private void SetupBattlePhaseControls()
        {
            _battlePhaseManager = new BattlePhaseManager(_hasAllies);

            _phaseLabel = CreateLabel($"Battle Phase:  {FormatName(_battlePhaseManager.CurrentPhase.ToString())}");

            CreateButton("Advance Phase", () =>
            {
                _battlePhaseManager.AdvancePhase();
                _phaseLabel.text = $"Battle Phase:  {FormatName(_battlePhaseManager.CurrentPhase.ToString())}";
            });

            CreateToggle("Has Allies", _hasAllies, value =>
            {
                _hasAllies = value;
                _battlePhaseManager.SetHasAllies(value);
            });

            CreateSpacer(20f);
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var buttonGo = new GameObject(label, typeof(RectTransform));
            buttonGo.transform.SetParent(_buttonContainer, false);

            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            var layout = buttonGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 50;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI CreateLabel(string text)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(_buttonContainer, false);

            var rect = labelGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 35);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.85f, 0.85f);
            tmp.raycastTarget = false;

            var layout = labelGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 35;

            return tmp;
        }

        private void CreateToggle(string label, bool initialValue, Action<bool> onValueChanged)
        {
            var toggleGo = new GameObject(label, typeof(RectTransform));
            toggleGo.transform.SetParent(_buttonContainer, false);

            var toggleRect = toggleGo.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(0, 40);

            var layout = toggleGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;

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

        private void CreateSpacer(float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(_buttonContainer, false);
            var layout = spacer.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
        }

        private static string FormatName(string pascalCase)
        {
            return Regex.Replace(pascalCase, @"(?<!^)([A-Z])", " $1");
        }
    }
}
