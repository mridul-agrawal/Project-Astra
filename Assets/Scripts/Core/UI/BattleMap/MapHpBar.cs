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
        private Canvas _canvas;
        private RectTransform _fillRect;
        private Image _fill;
        private Coroutine _drainCo;
        private float _currentRatio = 1f;
        private float _maxHpCached = 1f;

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
            _maxHpCached = Mathf.Max(1, maxHp);
            _currentRatio = Mathf.Clamp01((float)currentHp / _maxHpCached);
            if (_drainCo != null) { StopCoroutine(_drainCo); _drainCo = null; }
            ApplyRatio(_currentRatio);
            _canvas.gameObject.SetActive(true);
        }

        public void DrainTo(int targetHp, float durationSeconds)
        {
            if (_canvas == null) return;
            if (_drainCo != null) StopCoroutine(_drainCo);
            _drainCo = StartCoroutine(DrainCoroutine(targetHp, Mathf.Max(0f, durationSeconds)));
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private IEnumerator DrainCoroutine(int targetHp, float duration)
        {
            float startRatio = _currentRatio;
            float targetRatio = Mathf.Clamp01((float)targetHp / _maxHpCached);
            if (duration <= 0f)
            {
                _currentRatio = targetRatio;
                ApplyRatio(_currentRatio);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _currentRatio = Mathf.Lerp(startRatio, targetRatio, p);
                ApplyRatio(_currentRatio);
                yield return null;
            }
            _currentRatio = targetRatio;
            ApplyRatio(_currentRatio);
        }

        private void ApplyRatio(float ratio)
        {
            if (_fillRect == null || _fill == null) return;
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;
            _fill.color = ratio > 0.5f
                ? new Color(0.376f, 0.784f, 0.439f)   // green
                : ratio > 0.25f
                    ? new Color(0.901f, 0.776f, 0.207f)  // yellow
                    : new Color(0.901f, 0.282f, 0.282f); // red
        }

        private void EnsureBuilt()
        {
            if (_canvas != null) return;

            var go = new GameObject("MapHpBar");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingLayerName = "UIOverlay";
            _canvas.sortingOrder = 90;

            var rt = _canvas.GetComponent<RectTransform>();
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
            _fill = fillGo.AddComponent<Image>();
            _fill.sprite = sprite;
            _fill.color = new Color(0.376f, 0.784f, 0.439f);
            _fill.raycastTarget = false;
            _fillRect = fillGo.GetComponent<RectTransform>();
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(1f, 1f);
            _fillRect.offsetMin = new Vector2(2f, 2f);
            _fillRect.offsetMax = new Vector2(-2f, -2f);

            go.SetActive(false);
        }

        private static Sprite _solidSprite;
        private static Sprite SolidSprite()
        {
            if (_solidSprite != null) return _solidSprite;
            var tex = Texture2D.whiteTexture;
            _solidSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return _solidSprite;
        }
    }
}
