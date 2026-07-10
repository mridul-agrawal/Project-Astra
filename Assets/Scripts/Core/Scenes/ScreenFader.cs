using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Scenes
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private SceneFadeSettings sceneFadeSettings;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void RunTransition(Action onBlack) => StartCoroutine(Transition(onBlack));

        private IEnumerator Transition(Action onBlack)
        {
            canvasGroup.blocksRaycasts = true;
            AudioManager.Instance?.Play(SoundId.TransitionWhoosh);
            yield return Fade(sceneFadeSettings.Opaque);

            onBlack?.Invoke();
            yield return null;          // give the freshly loaded scene a frame to wake up

            yield return Fade(sceneFadeSettings.Transparent);
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float target)
        {
            float duration = sceneFadeSettings.FadeDuration;
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Cap the step so a scene-load hitch (a huge single delta) can't finish the
                // fade in one frame — the fade-in always animates regardless of load time.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, sceneFadeSettings.MaxFadeStep);
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
        }
    }
}
