using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    public class PhaseBannerUI : MonoBehaviour
    {
        static readonly Color PlayerAccent = new(0.831f, 0.635f, 0.298f, 1f);
        static readonly Color EnemyAccent  = new(0.753f, 0.271f, 0.220f, 1f);
        static readonly Color AlliedAccent = new(0.165f, 0.541f, 0.290f, 1f);

        static readonly Color PlayerTextColor = new(0.961f, 0.824f, 0.478f, 1f);
        static readonly Color EnemyTextColor  = new(0.961f, 0.627f, 0.541f, 1f);
        static readonly Color AlliedTextColor = new(0.627f, 0.910f, 0.667f, 1f);

        static readonly Color TurnTextColor = new(0.541f, 0.478f, 0.345f, 1f);

        [Header("Event Channel")]
        [SerializeField] private TurnEventChannel _turnEventChannel;

        [Header("UI References")]
        [SerializeField] private RectTransform _bannerRoot;
        [SerializeField] private Image _borderTop;
        [SerializeField] private Image _borderBottom;
        [SerializeField] private Image _innerBorderTop;
        [SerializeField] private Image _innerBorderBottom;
        [SerializeField] private Image[] _orbImages;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _turnText;
        [SerializeField] private Image _dimOverlay;

        [Header("Phase Materials")]
        [SerializeField] private Material _playerGlowMat;
        [SerializeField] private Material _enemyGlowMat;
        [SerializeField] private Material _alliedGlowMat;

        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _dimAlpha = 0.55f;
        static readonly Color DimColor = new(0.0196f, 0.0118f, 0.0196f, 0f);

        private void Awake()
        {
            if (_bannerRoot != null)
                _bannerRoot.gameObject.SetActive(false);
            if (_dimOverlay != null)
            {
                _dimOverlay.color = DimColor;
                _dimOverlay.gameObject.SetActive(false);
            }
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
            ApplyPhaseVisuals(phase, turnNumber);
            if (_bannerRoot == null) yield break;

            _bannerRoot.gameObject.SetActive(true);
            if (_dimOverlay != null) _dimOverlay.gameObject.SetActive(true);

            yield return AnimateSlideAndDim(-Screen.width, 0, 0f, _dimAlpha, _slideDuration);
            yield return new WaitForSeconds(_holdDuration);
            yield return AnimateSlideAndDim(0, Screen.width, _dimAlpha, 0f, _slideDuration);

            _bannerRoot.gameObject.SetActive(false);
            if (_dimOverlay != null) _dimOverlay.gameObject.SetActive(false);
        }

        private IEnumerator AnimateSlideAndDim(float fromX, float toX,
            float fromDim, float toDim, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _bannerRoot.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), 0f);
                SetDimAlpha(Mathf.Lerp(fromDim, toDim, t));
                yield return null;
            }
            _bannerRoot.anchoredPosition = new Vector2(toX, 0f);
            SetDimAlpha(toDim);
        }

        private void SetDimAlpha(float alpha)
        {
            if (_dimOverlay == null) return;
            var c = _dimOverlay.color;
            c.a = alpha;
            _dimOverlay.color = c;
        }

        private void ApplyPhaseVisuals(BattlePhase phase, int turnNumber)
        {
            var accent = GetAccentColor(phase);
            var textColor = GetTextColor(phase);
            var glowMat = GetGlowMaterial(phase);

            if (_phaseText == null) return;

            _phaseText.text = GetPhaseLabel(phase);
            _phaseText.color = textColor;
            if (glowMat != null) _phaseText.fontMaterial = glowMat;

            if (_turnText != null)
                _turnText.text = phase == BattlePhase.PlayerPhase ? $"Turn {turnNumber}" : "";

            if (_borderTop != null) _borderTop.color = WithAlpha(accent, 0.55f);
            if (_borderBottom != null) _borderBottom.color = WithAlpha(accent, 0.55f);
            if (_innerBorderTop != null) _innerBorderTop.color = WithAlpha(accent, 0.2f);
            if (_innerBorderBottom != null) _innerBorderBottom.color = WithAlpha(accent, 0.2f);

            if (_orbImages != null)
                foreach (var orb in _orbImages)
                    if (orb != null) orb.color = accent;
        }

        private static string GetPhaseLabel(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => "Player Phase",
                BattlePhase.EnemyPhase => "Enemy Phase",
                BattlePhase.AlliedPhase => "Allied Phase",
                _ => ""
            };
        }

        private static Color GetAccentColor(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => PlayerAccent,
                BattlePhase.EnemyPhase => EnemyAccent,
                BattlePhase.AlliedPhase => AlliedAccent,
                _ => PlayerAccent
            };
        }

        private static Color GetTextColor(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => PlayerTextColor,
                BattlePhase.EnemyPhase => EnemyTextColor,
                BattlePhase.AlliedPhase => AlliedTextColor,
                _ => Color.white
            };
        }

        private Material GetGlowMaterial(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => _playerGlowMat,
                BattlePhase.EnemyPhase => _enemyGlowMat,
                BattlePhase.AlliedPhase => _alliedGlowMat,
                _ => _playerGlowMat
            };
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
