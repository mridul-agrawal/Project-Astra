using System.Collections;
using UnityEngine;
using TMPro;

namespace ProjectAstra.Core.UI
{
    public class PhaseBannerUI : MonoBehaviour
    {
        private static readonly Color PlayerPhaseColor = new(0.2f, 0.4f, 0.9f, 1f);
        private static readonly Color EnemyPhaseColor = new(0.85f, 0.15f, 0.15f, 1f);
        private static readonly Color AlliedPhaseColor = new(0.2f, 0.75f, 0.3f, 1f);

        [SerializeField] private TurnEventChannel _turnEventChannel;

        const float SlideDuration = 0.3f;
        const float HoldDuration = 1.2f;
        const float BannerHeight = 40f;

        private GameObject _bannerObject;
        private TextMeshProUGUI _text;
        private RectTransform _bannerRect;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            CreateBannerUI();
            _bannerObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_turnEventChannel != null)
                _turnEventChannel.RegisterPhaseStarted(OnPhaseStarted);
        }

        private void OnDisable()
        {
            if (_turnEventChannel != null)
                _turnEventChannel.UnregisterPhaseStarted(OnPhaseStarted);
        }

        private void OnPhaseStarted(BattlePhase phase, int turnNumber)
        {
            StopAllCoroutines();
            StartCoroutine(ShowBanner(phase, turnNumber));
        }

        private IEnumerator ShowBanner(BattlePhase phase, int turnNumber)
        {
            _text.text = GetPhaseText(phase, turnNumber);
            _text.color = GetPhaseColor(phase);
            _bannerObject.SetActive(true);

            // Slide in from left
            yield return SlideHorizontal(-Screen.width, 0, SlideDuration);

            // Hold
            yield return new WaitForSeconds(HoldDuration);

            // Slide out to right
            yield return SlideHorizontal(0, Screen.width, SlideDuration);

            _bannerObject.SetActive(false);
        }

        private IEnumerator SlideHorizontal(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _bannerRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), 0f);
                yield return null;
            }
            _bannerRect.anchoredPosition = new Vector2(toX, 0f);
        }

        private static string GetPhaseText(BattlePhase phase, int turn)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => $"Player Phase  -  Turn {turn}",
                BattlePhase.EnemyPhase => "Enemy Phase",
                BattlePhase.AlliedPhase => "Allied Phase",
                _ => ""
            };
        }

        private static Color GetPhaseColor(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => PlayerPhaseColor,
                BattlePhase.EnemyPhase => EnemyPhaseColor,
                BattlePhase.AlliedPhase => AlliedPhaseColor,
                _ => Color.white
            };
        }

        private void CreateBannerUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }

            _bannerObject = new GameObject("PhaseBanner");
            _bannerObject.transform.SetParent(canvas.transform, false);

            _bannerRect = _bannerObject.AddComponent<RectTransform>();
            _bannerRect.anchorMin = new Vector2(0f, 0.4f);
            _bannerRect.anchorMax = new Vector2(1f, 0.6f);
            _bannerRect.offsetMin = Vector2.zero;
            _bannerRect.offsetMax = Vector2.zero;

            var bg = _bannerObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            _canvasGroup = _bannerObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;

            var textObj = new GameObject("PhaseText");
            textObj.transform.SetParent(_bannerObject.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = textObj.AddComponent<TextMeshProUGUI>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontSize = 24;
            _text.fontStyle = FontStyles.Bold;
            _text.enableWordWrapping = false;
        }
    }
}
