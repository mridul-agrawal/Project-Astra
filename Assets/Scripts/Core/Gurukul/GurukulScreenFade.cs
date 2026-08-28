using System;
using System.Collections;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // The short fade a doorway hides behind.
    //
    // Its own thing rather than the shared ScreenFader, for two reasons: that one marks itself
    // DontDestroyOnLoad, so a hub-spawned copy would follow the player into the battle map, and its
    // duration comes from the scene-load settings — 0.35s, where a doorway wants about 0.2s.
    //
    // The per-frame clamp is kept, though. Without it a hitch while a room is built can hand the
    // fade one enormous delta and finish it in a single frame, so the fade-in never animates.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GurukulScreenFade : MonoBehaviour
    {
        private const float MaxFadeStep = 1f / 30f;

        [Tooltip("Seconds to fade out, and again to fade back in. The spec's doorway target is about 0.2 each way.")]
        [SerializeField] private float fadeDuration = 0.2f;

        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public bool IsFading { get; private set; }

        // Fades to black, runs the swap, then fades back. The caller yields on this so control
        // doesn't come back until the new room is fully on screen.
        public IEnumerator Cover(Action whileBlack)
        {
            IsFading = true;
            group.blocksRaycasts = true;

            yield return FadeTo(1f);
            whileBlack?.Invoke();
            yield return null;              // let the new room's objects wake up before it is seen

            yield return FadeTo(0f);
            group.blocksRaycasts = false;
            IsFading = false;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
                group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
