using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Cursor
{
    // Paints tile-set overlays for cursor feedback — movement range while a
    // unit is selected, attack range while targeting, heal range for staff
    // users. Pulses a low-amplitude shimmer over whatever is currently shown.
    public class RangeHighlighter : MonoBehaviour
    {
        private static readonly Color MovementColor = new(0.25f, 0.4f, 1.0f, 0.7f);
        private static readonly Color PassThroughColor = new(0.2f, 0.7f, 1.0f, 0.45f);
        private static readonly Color AttackColor = new(1.0f, 0.2f, 0.15f, 0.7f);
        private static readonly Color HealColor = new(0.15f, 0.85f, 0.3f, 0.7f);

        const float ShimmerFrequency = 1.5f;
        const float ShimmerAmplitude = 0.15f;

        private readonly List<GameObject> activeOverlays = new();
        private readonly List<Color> baseColors = new();
        private readonly Queue<GameObject> pool = new();
        private Sprite overlaySprite;
        private Transform overlayContainer;
        private Coroutine shimmerCoroutine;
        private float shimmerPhase;

        private void Awake()
        {
            overlaySprite = OverlaySpriteFactory.GetOverlaySprite();
            overlayContainer = new GameObject("RangeOverlays").transform;
        }

        private void OnDestroy()
        {
            if (overlayContainer != null)
                Destroy(overlayContainer.gameObject);
        }

        public void ShowMovementRange(HashSet<Vector2Int> destinations, HashSet<Vector2Int> passThrough)
        {
            ClearAll();

            foreach (var tile in destinations)
                PlaceOverlay(tile, MovementColor);

            if (passThrough != null)
                foreach (var tile in passThrough)
                    PlaceOverlay(tile, PassThroughColor);

            StartShimmer();
        }

        public void ShowAttackRange(HashSet<Vector2Int> attackable) =>
            ShowSingleColorRange(attackable, AttackColor);

        public void ShowHealRange(HashSet<Vector2Int> healable) =>
            ShowSingleColorRange(healable, HealColor);

        public void ClearAll()
        {
            StopShimmer();

            foreach (var overlay in activeOverlays)
            {
                overlay.SetActive(false);
                pool.Enqueue(overlay);
            }
            activeOverlays.Clear();
            baseColors.Clear();
        }

        private void ShowSingleColorRange(HashSet<Vector2Int> tiles, Color color)
        {
            ClearAll();

            foreach (var tile in tiles)
                PlaceOverlay(tile, color);

            StartShimmer();
        }

        private void PlaceOverlay(Vector2Int tile, Color color)
        {
            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            overlay.SetActive(true);

            var sr = overlay.GetComponent<SpriteRenderer>();
            sr.color = color;

            activeOverlays.Add(overlay);
            baseColors.Add(color);
        }

        private GameObject GetOrCreateOverlay()
        {
            if (pool.Count > 0)
                return pool.Dequeue();

            var overlay = new GameObject("RangeOverlay");
            overlay.transform.SetParent(overlayContainer);

            var sr = overlay.AddComponent<SpriteRenderer>();
            sr.sprite = overlaySprite;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = -1;

            return overlay;
        }

        // --- Shimmer pulse ---

        private void StartShimmer()
        {
            if (shimmerCoroutine == null)
                shimmerCoroutine = StartCoroutine(ShimmerLoop());
        }

        private void StopShimmer()
        {
            if (shimmerCoroutine != null)
            {
                StopCoroutine(shimmerCoroutine);
                shimmerCoroutine = null;
            }
        }

        private IEnumerator ShimmerLoop()
        {
            while (true)
            {
                shimmerPhase += Time.deltaTime * ShimmerFrequency;
                float alphaMultiplier = 1.0f + ShimmerAmplitude * Mathf.Sin(shimmerPhase * Mathf.PI * 2f);

                for (int i = 0; i < activeOverlays.Count; i++)
                {
                    if (!activeOverlays[i].activeSelf) continue;
                    var sr = activeOverlays[i].GetComponent<SpriteRenderer>();
                    Color c = baseColors[i];
                    c.a *= alphaMultiplier;
                    sr.color = c;
                }

                yield return null;
            }
        }
    }
}
