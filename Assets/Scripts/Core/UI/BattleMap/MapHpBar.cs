using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Tiny world-space HP bar that appears above a unit's map sprite during
    // Skip-mode combat. Lazy-built on first Show (no prefab). Hidden by
    // default; shown when combat begins, drained per hit, hidden ~0.3s after
    // the last hit.
    //
    // One instance per TestUnit, attached via GetOrCreate. The bar GameObject
    // is a child of the unit so it follows position automatically.
    public class MapHpBar : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform fillRect;
        private Image fill;
        private Coroutine drainCo;
        private float currentRatio = 1f;
        private float maxHpCached = 1f;

        private const float BarWorldWidth = 0.84f;
        private const float BarWorldHeight = 0.10f;

        public static MapHpBar GetOrCreate(TestUnit unit)
        {
            if (unit == null) return null;
            var bar = unit.GetComponent<MapHpBar>();
            if (bar == null) bar = unit.gameObject.AddComponent<MapHpBar>();
            return bar;
        }

        public void Show(int currentHp, int maxHp)
        {
            EnsureBuilt();
            maxHpCached = Mathf.Max(1, maxHp);
            currentRatio = Mathf.Clamp01((float)currentHp / maxHpCached);
            if (drainCo != null) { StopCoroutine(drainCo); drainCo = null; }
            ApplyRatio(currentRatio);
            canvas.gameObject.SetActive(true);
        }

        public void DrainTo(int targetHp, float durationSeconds)
        {
            if (canvas == null) return;
            if (drainCo != null) StopCoroutine(drainCo);
            drainCo = StartCoroutine(DrainCoroutine(targetHp, Mathf.Max(0f, durationSeconds)));
        }

        public void Hide()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private IEnumerator DrainCoroutine(int targetHp, float duration)
        {
            float startRatio = currentRatio;
            float targetRatio = Mathf.Clamp01((float)targetHp / maxHpCached);
            if (duration <= 0f)
            {
                currentRatio = targetRatio;
                ApplyRatio(currentRatio);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                currentRatio = Mathf.Lerp(startRatio, targetRatio, p);
                ApplyRatio(currentRatio);
                yield return null;
            }
            currentRatio = targetRatio;
            ApplyRatio(currentRatio);
        }

        private void ApplyRatio(float ratio)
        {
            if (fillRect == null || fill == null) return;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.color = ratio > 0.5f
                ? new Color(0.376f, 0.784f, 0.439f)   // green
                : ratio > 0.25f
                    ? new Color(0.901f, 0.776f, 0.207f)  // yellow
                    : new Color(0.901f, 0.282f, 0.282f); // red
        }

        private void EnsureBuilt()
        {
            if (canvas != null) return;

            var go = new GameObject("MapHpBar");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UIOverlay";
            canvas.sortingOrder = 90;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(BarWorldWidth * 100f, BarWorldHeight * 100f);
            rt.localScale = Vector3.one * 0.01f;

            var sprite = SolidSprite();

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            fill = fillGo.AddComponent<Image>();
            fill.sprite = sprite;
            fill.color = new Color(0.376f, 0.784f, 0.439f);
            fill.raycastTarget = false;
            fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            go.SetActive(false);
        }

        private static Sprite solidSprite;
        private static Sprite SolidSprite()
        {
            if (solidSprite != null) return solidSprite;
            var tex = Texture2D.whiteTexture;
            solidSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return solidSprite;
        }
    }
}
