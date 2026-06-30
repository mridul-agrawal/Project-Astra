using System.Collections;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap
{
    // World-space TMP symbols ("!" danger, "X" death) on the camera-rendered UIOverlay
    // sorting layer, so scripted sequences can mark units without art or text boxes.
    // Plain ASCII only — the default TMP font lacks special glyphs. The caller hosts
    // the Pulse / RiseAndFade coroutines and owns the spawned object's lifetime.
    public static class WorldMarker
    {
        public static GameObject Spawn(Vector3 worldPos, string text, Color color, float fontSize)
        {
            var go = new GameObject("CinematicMarker");
            go.transform.position = worldPos + new Vector3(0f, 0.5f, 0f);  // above the unit's head

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UIOverlay";
            canvas.sortingOrder = 200;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 110f);
            rect.localScale = Vector3.one * 0.018f;

            go.AddComponent<CanvasGroup>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.outlineColor = Color.black;
            tmp.outlineWidth = 0.25f;
            tmp.raycastTarget = false;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return go;
        }

        public static IEnumerator Pulse(Transform target)
        {
            Vector3 baseScale = target.localScale;
            float time = 0f;
            while (target != null)
            {
                time += Time.deltaTime;
                target.localScale = baseScale * (1f + 0.2f * Mathf.Sin(time * 9f));
                yield return null;
            }
        }

        public static IEnumerator RiseAndFade(GameObject marker, float duration, float rise)
        {
            var group = marker.GetComponent<CanvasGroup>();
            Vector3 start = marker.transform.position;
            Vector3 end = start + Vector3.up * rise;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (marker == null) yield break;
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                marker.transform.position = Vector3.Lerp(start, end, p);
                if (group != null) group.alpha = 1f - p;
                yield return null;
            }
            if (marker != null) Object.Destroy(marker);
        }
    }
}
