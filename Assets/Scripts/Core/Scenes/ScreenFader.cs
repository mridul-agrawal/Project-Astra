using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Scenes
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private SceneFadeSettings fadeSettings;
        private CanvasGroup canvasGroup;

        // Largest time step a single frame may contribute to a fade. Keeps a load-frame hitch
        // from snapping the fade straight to its end. A technical frame-pacing guard, not
        // designer feel, so it stays in code.
        private const float MaxFadeStep = 1f / 30f;

        private const float FallbackFadeDuration = 0.35f;

        // The overlay's two alpha endpoints: fully opaque (black, hides the game) and
        // fully transparent (the game shows through).
        private const float Opaque = 1f;
        private const float Transparent = 0f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Fade to black, run the swap at full black, then fade back in. Blocks input while covered.
        public void RunTransition(Action onBlack) => StartCoroutine(Transition(onBlack));

        private IEnumerator Transition(Action onBlack)
        {
            canvasGroup.blocksRaycasts = true;
            AudioManager.Instance?.Play(SoundId.TransitionWhoosh);
            yield return Fade(Opaque);

            onBlack?.Invoke();
            yield return null;          // give the freshly loaded scene a frame to wake up

            yield return Fade(Transparent);
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float target)
        {
            float duration = fadeSettings != null ? fadeSettings.FadeDuration : FallbackFadeDuration;
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Cap the step so a scene-load hitch (a huge single delta) can't finish the
                // fade in one frame — the fade-in always animates regardless of load time.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
        }
    }
}
