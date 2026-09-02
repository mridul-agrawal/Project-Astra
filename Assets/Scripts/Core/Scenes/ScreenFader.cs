using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Scenes
{
    // The game's one blackout. Fades to black, runs something while nothing can be seen, fades
    // back. A scene swap hides behind it, and so does a hub doorway swapping one room for another.
    //
    // SceneLoader creates exactly one at boot and it lives for the session, so anything that needs
    // to hide a moment behind black asks this rather than building its own overlay.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        // "Enter Play Mode" with domain reload off leaves statics set between sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [SerializeField] private SceneFadeSettings sceneFadeSettings;
        private CanvasGroup canvasGroup;

        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            DontDestroyOnLoad(gameObject);
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Fire-and-forget, for a caller that has nothing to do afterwards.
        public void RunTransition(Action onBlack) => StartCoroutine(Cover(onBlack));

        // Yield on this and control comes back only once the screen is visible again — which is
        // what a doorway needs, since the player must not be able to walk during the swap.
        //
        // A doorway is shorter than a scene load and silent: the whoosh belongs to a change of
        // place, not to every door in a courtyard.
        public IEnumerator Cover(Action whileBlack, float durationOverride = -1f, bool playSound = true)
        {
            IsFading = true;
            canvasGroup.blocksRaycasts = true;

            float duration = durationOverride > 0f ? durationOverride : sceneFadeSettings.FadeDuration;
            if (playSound) AudioManager.Instance?.Play(SoundId.TransitionWhoosh);

            yield return Fade(sceneFadeSettings.Opaque, duration);

            whileBlack?.Invoke();
            yield return null;          // give whatever was just built a frame to wake up

            yield return Fade(sceneFadeSettings.Transparent, duration);
            canvasGroup.blocksRaycasts = false;
            IsFading = false;
        }

        private IEnumerator Fade(float target, float duration)
        {
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
