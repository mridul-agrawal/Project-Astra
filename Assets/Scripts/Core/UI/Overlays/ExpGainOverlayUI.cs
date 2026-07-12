using System.Collections;
using TMPro;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Overlays
{
    // Brief animated counter shown after an EXP-granting action. Counts the
    // acting unit's EXP from its pre-grant total to (total + amount),
    // wrapping at 100 if the threshold is crossed so the player sees the
    // roll-over.
    //
    // Non-blocking — state stays on BattleMap. The level-up UI is a separate
    // state-transitioned screen; ExpGranter sequences the two.
    public class ExpGainOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private TMP_Text gainText;

        [Header("Timing")]
        [SerializeField] private float fadeInSeconds = 0.2f;
        [SerializeField] private float countSeconds = 0.6f;
        [SerializeField] private float holdSeconds = 0.3f;
        [SerializeField] private float fadeOutSeconds = 0.2f;

        public IEnumerator Play(TestUnit recipient, int preExp, int amount)
        {
            if (overlayRoot != null) overlayRoot.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            if (unitNameText != null)
                unitNameText.text = recipient != null ? recipient.name : "";

            if (gainText != null)
                gainText.text = $"+{amount} EXP";

            SetCounter(preExp);

            yield return FadeTo(1f, fadeInSeconds);

            yield return CountUp(preExp, preExp + amount);

            yield return new WaitForSeconds(holdSeconds);
            yield return FadeTo(0f, fadeOutSeconds);

            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (canvasGroup == null || duration <= 0f)
            {
                if (canvasGroup != null) canvasGroup.alpha = target;
                yield break;
            }
            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.SmoothStep(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        private IEnumerator CountUp(int from, int to)
        {
            if (counterText == null || countSeconds <= 0f)
            {
                SetCounter(to % UnitInstance.ExpPerLevel);
                yield break;
            }

            float t = 0f;
            float tickTimer = 0f;
            int lastShown = from;
            while (t < countSeconds)
            {
                t += Time.deltaTime;
                tickTimer += Time.deltaTime;
                float p = Mathf.Clamp01(t / countSeconds);
                int shown = Mathf.RoundToInt(Mathf.Lerp(from, to, p));
                SetCounter(shown % UnitInstance.ExpPerLevel);
                if (shown != lastShown && tickTimer >= 0.05f)
                {
                    AudioManager.Instance?.Play(SoundId.ExpTick);
                    tickTimer = 0f;
                }
                lastShown = shown;
                yield return null;
            }
            SetCounter(to % UnitInstance.ExpPerLevel);
        }

        private void SetCounter(int value)
        {
            if (counterText != null)
                counterText.text = $"{value} / {UnitInstance.ExpPerLevel}";
        }
    }
}
