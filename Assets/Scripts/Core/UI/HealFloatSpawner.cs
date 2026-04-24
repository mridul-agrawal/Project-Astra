using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Spawns a brief world-space "+N" text above a unit when a heal resolves.
    /// Runtime-built (no prefab) to avoid one-shot overlay infrastructure —
    /// the float is simple enough that a coroutine-driven fade-up suffices.
    /// Silent for no-op heals; HealingTileSystem gates the call on gained > 0.
    /// </summary>
    public class HealFloatSpawner : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float _floatUpSeconds = 1.2f;
        [SerializeField] private float _holdSeconds = 0.3f;
        [SerializeField] private float _riseDistance = 0.6f;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.6f, 0f);

        [Header("Text")]
        [SerializeField] private int _fontSize = 48;
        [SerializeField] private Color _textColor = new Color(0.486f, 0.827f, 0.416f, 1f);
        [SerializeField] private Color _outlineColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private float _canvasScale = 0.025f;

        [Header("Layering")]
        [Tooltip("Sprite sort layer the float renders on. Match the top-most in-scene sprite layer so tiles/units never occlude.")]
        [SerializeField] private string _sortingLayerName = "UIOverlay";
        [SerializeField] private int _sortingOrder = 100;

        public void Show(Vector3 worldPos, int amount)
        {
            if (amount <= 0) return;
            StartCoroutine(PlayFloat(worldPos + _spawnOffset, amount));
        }

        private IEnumerator PlayFloat(Vector3 startWorldPos, int amount)
        {
            var go = new GameObject($"HealFloat(+{amount})");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = startWorldPos;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (!string.IsNullOrEmpty(_sortingLayerName))
                canvas.sortingLayerName = _sortingLayerName;
            canvas.sortingOrder = _sortingOrder;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 60f);
            rt.localScale = Vector3.one * _canvasScale;

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"+{amount}";
            tmp.fontSize = _fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = _textColor;
            tmp.outlineColor = _outlineColor;
            tmp.outlineWidth = 0.25f;
            tmp.raycastTarget = false;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Hold at full alpha briefly so the player can read the number, then rise + fade.
            float tHold = 0f;
            while (tHold < _holdSeconds)
            {
                tHold += Time.deltaTime;
                yield return null;
            }

            Vector3 endPos = startWorldPos + Vector3.up * _riseDistance;
            float t = 0f;
            while (t < _floatUpSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _floatUpSeconds);
                go.transform.position = Vector3.Lerp(startWorldPos, endPos, p);
                cg.alpha = 1f - p;
                yield return null;
            }

            Destroy(go);
        }
    }
}
