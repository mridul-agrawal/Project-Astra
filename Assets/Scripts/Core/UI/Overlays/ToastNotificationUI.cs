using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.Overlays
{
    // Brief slide-in / hold / slide-out toast for short notifications like
    // "Iron Sword broke!". Modeled on PhaseBannerUI's coroutine animation.
    public class ToastNotificationUI : MonoBehaviour
    {
        const float SlideDuration = 0.2f;
        const float HoldDuration = 1.5f;
        const float ToastHeight = 36f;

        private GameObject toastObject;
        private RectTransform toastRect;
        private TextMeshProUGUI text;

        private void Awake()
        {
            CreateUI();
            if (toastObject != null) toastObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (toastObject == null) CreateUI();
            if (toastObject == null) return;

            StopAllCoroutines();
            text.text = message;
            toastObject.SetActive(true);
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return Slide(Screen.width * 0.5f, 0f, SlideDuration);
            yield return new WaitForSeconds(HoldDuration);
            yield return Slide(0f, -Screen.width * 0.5f, SlideDuration);
            toastObject.SetActive(false);
        }

        private IEnumerator Slide(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                toastRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), -80f);
                yield return null;
            }
            toastRect.anchoredPosition = new Vector2(toX, -80f);
        }

        private void CreateUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            toastObject = new GameObject("Toast");
            toastObject.transform.SetParent(canvas.transform, false);

            toastRect = toastObject.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = new Vector2(320f, ToastHeight);

            var bg = toastObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            var textObj = new GameObject("ToastText");
            textObj.transform.SetParent(toastObject.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.85f, 0.5f, 1f);
            text.enableWordWrapping = false;

            toastObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }
    }
}
