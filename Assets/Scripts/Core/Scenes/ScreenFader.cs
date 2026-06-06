using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.Scenes
{
    // A persistent full-screen black overlay that covers scene swaps. SceneLoader asks it to
    // fade to black, run the load, then fade back in — so transitions read as a smooth crossfade
    // instead of an instant pop. Lives across scene loads (DontDestroyOnLoad) and builds its own
    // UI, so nothing in a scene needs to wire it up.
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private float _fadeDuration = 0.35f;

        // Largest time step a single frame may contribute to a fade. Keeps a load-frame hitch
        // from snapping the fade straight to its end.
        private const float MaxFadeStep = 1f / 30f;

        private CanvasGroup _group;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        // Fade to black, run the swap at full black, then fade back in. Blocks input while covered.
        public void RunTransition(Action onBlack)
        {
            StartCoroutine(Transition(onBlack));
        }

        private IEnumerator Transition(Action onBlack)
        {
            _group.blocksRaycasts = true;
            yield return Fade(1f);

            onBlack?.Invoke();
            yield return null;          // give the freshly loaded scene a frame to wake up

            yield return Fade(0f);
            _group.blocksRaycasts = false;
        }

        private IEnumerator Fade(float target)
        {
            float start = _group.alpha;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                // Cap the step so a scene-load hitch (a huge single delta) can't finish the
                // fade in one frame — the fade-in always animates regardless of load time.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
                _group.alpha = Mathf.Lerp(start, target, elapsed / _fadeDuration);
                yield return null;
            }
            _group.alpha = target;
        }

        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;   // above every other canvas, including overlays

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var blackGo = new GameObject("Black", typeof(RectTransform));
            blackGo.transform.SetParent(transform, false);
            var image = blackGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var rect = blackGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
