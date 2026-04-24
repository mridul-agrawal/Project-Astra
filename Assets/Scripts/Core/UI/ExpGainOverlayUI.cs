using System.Collections;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Brief animated counter shown after an EXP-granting action. Counts the
    /// acting unit's EXP from its pre-grant total to (total + amount), wrapping
    /// at 100 if the threshold is crossed so the player sees the roll-over.
    ///
    /// Drives a non-blocking overlay — state stays on BattleMap. Level-up UI is
    /// a separate state-transitioned screen; the orchestrator (ExpGranter)
    /// sequences them.
    /// </summary>
    public class ExpGainOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _unitNameText;
        [SerializeField] private TMP_Text _counterText;
        [SerializeField] private TMP_Text _gainText;

        [Header("Timing")]
        [SerializeField] private float _fadeInSeconds = 0.2f;
        [SerializeField] private float _countSeconds = 0.6f;
        [SerializeField] private float _holdSeconds = 0.3f;
        [SerializeField] private float _fadeOutSeconds = 0.2f;

        public IEnumerator Play(TestUnit recipient, int preExp, int amount)
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            if (_unitNameText != null)
                _unitNameText.text = recipient != null ? recipient.name : "";

            if (_gainText != null)
                _gainText.text = $"+{amount} EXP";

            SetCounter(preExp);

            yield return FadeTo(1f, _fadeInSeconds);

            yield return CountUp(preExp, preExp + amount);

            yield return new WaitForSeconds(_holdSeconds);
            yield return FadeTo(0f, _fadeOutSeconds);

            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (_canvasGroup == null || duration <= 0f)
            {
                if (_canvasGroup != null) _canvasGroup.alpha = target;
                yield break;
            }
            float start = _canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.SmoothStep(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _canvasGroup.alpha = target;
        }

        private IEnumerator CountUp(int from, int to)
        {
            if (_counterText == null || _countSeconds <= 0f)
            {
                SetCounter(to % UnitInstance.ExpPerLevel);
                yield break;
            }

            float t = 0f;
            while (t < _countSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _countSeconds);
                int shown = Mathf.RoundToInt(Mathf.Lerp(from, to, p));
                SetCounter(shown % UnitInstance.ExpPerLevel);
                yield return null;
            }
            SetCounter(to % UnitInstance.ExpPerLevel);
        }

        private void SetCounter(int value)
        {
            if (_counterText != null)
                _counterText.text = $"{value} / {UnitInstance.ExpPerLevel}";
        }
    }
}
