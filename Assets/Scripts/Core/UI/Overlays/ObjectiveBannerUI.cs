using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.Overlays
{
    // Center-screen "OBJECTIVE CHANGED" banner: dark band slides in, holds, fades out.
    // Procedurally built on first use (no scene hierarchy to author); scripted
    // sequences call Show and yield on the returned coroutine.
    public class ObjectiveBannerUI : MonoBehaviour
    {
        private static readonly Color BandColor = new(0.07f, 0.06f, 0.05f, 0.92f);
        private static readonly Color AccentGold = new(1f, 0.78f, 0.25f, 1f);
        private static readonly Color TitleColor = new(0.9f, 0.85f, 0.75f, 1f);

        private const float SlideSeconds = 0.35f;
        private const float FadeSeconds = 0.4f;
        private const float BandHeight = 150f;
        private const float SlideDistance = 60f;

        private CanvasGroup group;
        private RectTransform band;
        private TMP_Text titleText;
        private TMP_Text subtitleText;

        public Coroutine Show(string title, string subtitle, float holdSeconds)
        {
            EnsureBuilt();
            titleText.text = title;
            subtitleText.text = subtitle;
            return StartCoroutine(Run(holdSeconds));
        }

        private IEnumerator Run(float holdSeconds)
        {
            group.alpha = 0f;
            Vector2 restPos = Vector2.zero;
            Vector2 startPos = restPos + Vector2.up * SlideDistance;

            float elapsed = 0f;
            while (elapsed < SlideSeconds)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SlideSeconds));
                band.anchoredPosition = Vector2.Lerp(startPos, restPos, p);
                group.alpha = p;
                yield return null;
            }

            yield return new WaitForSeconds(holdSeconds);

            elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / FadeSeconds);
                yield return null;
            }
            group.alpha = 0f;
        }

        // --- procedural build ---

        private void EnsureBuilt()
        {
            if (group != null) return;

            var canvasGo = new GameObject("ObjectiveBannerCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;   // above HUD, below the cinematic letterbox

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            band = BuildBand(canvasGo.transform);
            group = band.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            titleText = BuildText(band, "Title", 28f, AccentGold, new Vector2(0f, 28f));
            titleText.characterSpacing = 12f;
            subtitleText = BuildText(band, "Subtitle", 52f, TitleColor, new Vector2(0f, -22f));
        }

        private static RectTransform BuildBand(Transform parent)
        {
            var go = new GameObject("Band", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = BandColor;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, BandHeight);
            return rect;
        }

        private static TMP_Text BuildText(RectTransform parent, string name, float size, Color color, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 60f);
            rect.anchoredPosition = offset;
            return tmp;
        }
    }
}
