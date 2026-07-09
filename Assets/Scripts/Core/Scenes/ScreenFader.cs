using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Scenes
{
    // A persistent full-screen overlay that covers scene swaps. Its look (canvas + colour) is
    // authored on the ScreenFader prefab; its fade timing comes from SceneTransitionSettings, so
    // functional tuning stays separate from the view. Instantiated and owned by SceneLoader.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private SceneTransitionSettings _settings;

        // Largest time step a single frame may contribute to a fade. Keeps a load-frame hitch
        // from snapping the fade straight to its end. A technical frame-pacing guard, not
        // designer feel, so it stays in code.
        private const float MaxFadeStep = 1f / 30f;

        private const float FallbackFadeDuration = 0.35f;

        private CanvasGroup _group;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _group = GetComponent<CanvasGroup>();
        }

        // Fade to black, run the swap at full black, then fade back in. Blocks input while covered.
        public void RunTransition(Action onBlack) => StartCoroutine(Transition(onBlack));

        private IEnumerator Transition(Action onBlack)
        {
            _group.blocksRaycasts = true;
            AudioManager.Instance?.Play(SoundId.TransitionWhoosh);
            yield return Fade(1f);

            onBlack?.Invoke();
            yield return null;          // give the freshly loaded scene a frame to wake up

            yield return Fade(0f);
            _group.blocksRaycasts = false;
        }

        private IEnumerator Fade(float target)
        {
            float duration = _settings != null ? _settings.FadeDuration : FallbackFadeDuration;
            float start = _group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Cap the step so a scene-load hitch (a huge single delta) can't finish the
                // fade in one frame — the fade-in always animates regardless of load time.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
                _group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            _group.alpha = target;
        }
    }
}
