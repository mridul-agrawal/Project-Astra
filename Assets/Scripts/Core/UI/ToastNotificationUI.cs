using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Brief slide-in/hold/slide-out toast for short notifications like
    /// "Iron Sword broke!". Modeled on PhaseBannerUI's coroutine animation.
    /// </summary>
    public class ToastNotificationUI : MonoBehaviour
    {
        const float SlideDuration = 0.2f;
        const float HoldDuration = 1.5f;
        const float ToastHeight = 36f;

        private GameObject _toastObject;
        private RectTransform _toastRect;
        private TextMeshProUGUI _text;

        private void Awake()
        {
            CreateUI();
            if (_toastObject != null) _toastObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (_toastObject == null) CreateUI();
            if (_toastObject == null) return;

            StopAllCoroutines();
            _text.text = message;
            _toastObject.SetActive(true);
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return Slide(Screen.width * 0.5f, 0f, SlideDuration);
            yield return new WaitForSeconds(HoldDuration);
            yield return Slide(0f, -Screen.width * 0.5f, SlideDuration);
            _toastObject.SetActive(false);
        }

        private IEnumerator Slide(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _toastRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), -80f);
                yield return null;
            }
            _toastRect.anchoredPosition = new Vector2(toX, -80f);
        }

        private void CreateUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _toastObject = new GameObject("Toast");
            _toastObject.transform.SetParent(canvas.transform, false);

            _toastRect = _toastObject.AddComponent<RectTransform>();
            _toastRect.anchorMin = new Vector2(0.5f, 1f);
            _toastRect.anchorMax = new Vector2(0.5f, 1f);
            _toastRect.pivot = new Vector2(0.5f, 1f);
            _toastRect.sizeDelta = new Vector2(320f, ToastHeight);

            var bg = _toastObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            var textObj = new GameObject("ToastText");
            textObj.transform.SetParent(_toastObject.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            _text = textObj.AddComponent<TextMeshProUGUI>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontSize = 18;
            _text.fontStyle = FontStyles.Bold;
            _text.color = new Color(1f, 0.85f, 0.5f, 1f);
            _text.enableWordWrapping = false;

            _toastObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }
    }
}
