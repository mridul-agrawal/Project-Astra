using System.Collections;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Brief world-space damage / miss / crit text that pops above a unit
    // during Skip-mode combat. Mirrors the HealFloatSpawner pattern:
    // runtime-built world-space Canvas + TMP + CanvasGroup + a hold-then-
    // rise-fade coroutine. Lives on a CombatRuntime GameObject in the scene.
    public class MapDamageFloat : MonoBehaviour
    {
        public enum Kind { Damage, Miss, Crit }

        [Header("Motion")]
        [SerializeField] private float floatUpSeconds = 0.8f;
        [SerializeField] private float holdSeconds = 0.18f;
        [SerializeField] private float riseDistance = 0.65f;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.6f, 0f);

        [Header("Text")]
        [SerializeField] private int fontSize = 56;
        [SerializeField] private Color damageColor = new Color(0.96f, 0.88f, 0.85f, 1f);
        [SerializeField] private Color critColor = new Color(0.98f, 0.94f, 0.28f, 1f);
        [SerializeField] private Color missColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private float canvasScale = 0.025f;

        [Header("Layering")]
        [SerializeField] private string sortingLayerName = "UIOverlay";
        [SerializeField] private int sortingOrder = 100;

        public void Show(Vector3 worldPos, int amount, Kind kind)
        {
            StartCoroutine(PlayFloat(worldPos + spawnOffset, amount, kind));
        }

        private IEnumerator PlayFloat(Vector3 startWorldPos, int amount, Kind kind)
        {
            var go = new GameObject($"DamageFloat({(kind == Kind.Miss ? "Miss" : amount.ToString())})");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = startWorldPos;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (!string.IsNullOrEmpty(sortingLayerName)) canvas.sortingLayerName = sortingLayerName;
            canvas.sortingOrder = sortingOrder;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 70f);
            rt.localScale = Vector3.one * canvasScale;

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = kind == Kind.Miss
                ? "MISS"
                : kind == Kind.Crit
                    ? amount + "!"
                    : amount.ToString();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = kind == Kind.Crit ? critColor : kind == Kind.Miss ? missColor : damageColor;
            tmp.outlineColor = outlineColor;
            tmp.outlineWidth = 0.28f;
            tmp.raycastTarget = false;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Hold at full alpha so the player can read it, then rise + fade.
            float tHold = 0f;
            while (tHold < holdSeconds) { tHold += Time.deltaTime; yield return null; }

            Vector3 endPos = startWorldPos + Vector3.up * riseDistance;
            float t = 0f;
            while (t < floatUpSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / floatUpSeconds);
                go.transform.position = Vector3.Lerp(startWorldPos, endPos, p);
                cg.alpha = 1f - p;
                yield return null;
            }

            Destroy(go);
        }
    }
}
