using System;
using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // §10 screen and tab motion. Entry slides up 10 logical px over 150 ms, exit fades over 120,
    // and a tab swap slides its content in 8 px over 120. Driven by hand rather than by an
    // Animator so the exit can hold the panel open until the fade finishes.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UnitInfoScreenTransition : MonoBehaviour
    {
        const float Scale = 4f;          // the project canvas is 1920x1080 against a 480x270 spec

        [SerializeField] private RectTransform slider;
        [SerializeField] private float entrySeconds = 0.15f;
        [SerializeField] private float exitSeconds = 0.12f;
        [SerializeField] private float entryRise = 10f * Scale;

        [Header("Tab swap")]
        [SerializeField] private float tabSeconds = 0.12f;
        [SerializeField] private float tabSlide = 8f * Scale;

        private CanvasGroup fader;
        private Vector2 restingPosition;
        private Action onHidden;
        private float elapsed;
        private Phase phase = Phase.Idle;

        private enum Phase { Idle, Entering, Exiting }

        private void Awake()
        {
            fader = GetComponent<CanvasGroup>();
            if (slider == null) slider = GetComponent<RectTransform>();
            restingPosition = slider.anchoredPosition;
        }

        public void PlayEntry()
        {
            phase = Phase.Entering;
            elapsed = 0f;
            fader.alpha = 0f;
            slider.anchoredPosition = restingPosition - new Vector2(0f, entryRise);
        }

        // The caller hides the panel only once the fade is done, so the screen does not blink out.
        public void PlayExit(Action hidePanel)
        {
            phase = Phase.Exiting;
            elapsed = 0f;
            onHidden = hidePanel;
        }

        public void PlayTabSwap(RectTransform content)
        {
            if (content == null) return;
            StopAllCoroutines();
            StartCoroutine(SlideIn(content));
        }

        private System.Collections.IEnumerator SlideIn(RectTransform content)
        {
            var group = content.GetComponent<CanvasGroup>();
            Vector2 home = content.anchoredPosition;
            float time = 0f;

            while (time < tabSeconds)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(time / tabSeconds));
                content.anchoredPosition = home + new Vector2(tabSlide * (1f - t), 0f);
                if (group != null) group.alpha = t;
                yield return null;
            }

            content.anchoredPosition = home;
            if (group != null) group.alpha = 1f;
        }

        private void Update()
        {
            switch (phase)
            {
                case Phase.Entering: StepEntry(); break;
                case Phase.Exiting:  StepExit();  break;
            }
        }

        private void StepEntry()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOut(Mathf.Clamp01(elapsed / entrySeconds));

            fader.alpha = t;
            slider.anchoredPosition = restingPosition - new Vector2(0f, entryRise * (1f - t));
            if (t >= 1f) phase = Phase.Idle;
        }

        private void StepExit()
        {
            elapsed += Time.unscaledDeltaTime;
            fader.alpha = 1f - Mathf.Clamp01(elapsed / exitSeconds);
            if (fader.alpha > 0f) return;

            phase = Phase.Idle;
            slider.anchoredPosition = restingPosition;
            onHidden?.Invoke();
            onHidden = null;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
