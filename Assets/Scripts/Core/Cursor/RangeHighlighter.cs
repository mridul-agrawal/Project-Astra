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

        // How long a single tile takes to fade in once its turn in the flood comes round.
        const float TileFadeSeconds = 0.12f;

        private readonly List<GameObject> activeOverlays = new();
        private readonly List<SpriteRenderer> overlayRenderers = new();
        private readonly List<Color> baseColors = new();
        private readonly List<float> revealDelays = new();
        private readonly Queue<GameObject> pool = new();
        private Sprite overlaySprite;
        private Transform overlayContainer;
        private Coroutine shimmerCoroutine;
        private float shimmerPhase;
        private float floodElapsed;
        private float floodDuration;

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

        // The FE-style selection view: blue where the unit can move, red on the
        // attack fringe around it. Painted in one pass because each Show* clears
        // first, so two separate calls would wipe each other. Callers pass an
        // attack set already stripped of move tiles, so nothing double-paints.
        public void ShowMovementAndAttackRange(
            HashSet<Vector2Int> destinations, HashSet<Vector2Int> passThrough, HashSet<Vector2Int> attack)
        {
            ClearAll();

            if (attack != null)
                foreach (var tile in attack)
                    PlaceOverlay(tile, AttackColor);

            foreach (var tile in destinations)
                PlaceOverlay(tile, MovementColor);

            if (passThrough != null)
                foreach (var tile in passThrough)
                    PlaceOverlay(tile, PassThroughColor);

            StartShimmer();
        }

        // Same picture, revealed outward from the unit in Dijkstra cost order instead of all
        // at once, so the range reads as spreading rather than appearing. costMap comes
        // straight off the reachability result; the attack fringe always lands last because
        // it sits outside everything the unit can walk to.
        public void ShowMovementAndAttackRangeStaggered(
            HashSet<Vector2Int> destinations, HashSet<Vector2Int> passThrough, HashSet<Vector2Int> attack,
            IReadOnlyDictionary<Vector2Int, int> costMap, float flood)
        {
            if (flood <= 0f || costMap == null)
            {
                ShowMovementAndAttackRange(destinations, passThrough, attack);
                return;
            }

            ClearAll();
            floodDuration = flood;

            int maxCost = 1;
            foreach (var cost in costMap.Values)
                if (cost > maxCost) maxCost = cost;

            if (attack != null)
                foreach (var tile in attack)
                    PlaceOverlay(tile, AttackColor, flood);

            foreach (var tile in destinations)
                PlaceOverlay(tile, MovementColor, DelayFor(tile, costMap, maxCost, flood));

            if (passThrough != null)
                foreach (var tile in passThrough)
                    PlaceOverlay(tile, PassThroughColor, DelayFor(tile, costMap, maxCost, flood));

            StartShimmer();
        }

        private static float DelayFor(Vector2Int tile, IReadOnlyDictionary<Vector2Int, int> costMap,
            int maxCost, float flood) =>
            costMap.TryGetValue(tile, out int cost) ? flood * cost / maxCost : 0f;

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
            overlayRenderers.Clear();
            baseColors.Clear();
            revealDelays.Clear();
            floodElapsed = 0f;
            floodDuration = 0f;
        }

        private void ShowSingleColorRange(HashSet<Vector2Int> tiles, Color color)
        {
            ClearAll();

            foreach (var tile in tiles)
                PlaceOverlay(tile, color);

            StartShimmer();
        }

        private void PlaceOverlay(Vector2Int tile, Color color, float revealDelay = 0f)
        {
            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            overlay.SetActive(true);

            var sr = overlay.GetComponent<SpriteRenderer>();
            sr.color = revealDelay > 0f ? Transparent(color) : color;

            activeOverlays.Add(overlay);
            overlayRenderers.Add(sr);
            baseColors.Add(color);
            revealDelays.Add(revealDelay);
        }

        private static Color Transparent(Color color)
        {
            color.a = 0f;
            return color;
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

        // One loop drives both the shimmer and the flood-in, over cached renderers rather
        // than a GetComponent per tile per frame.
        private IEnumerator ShimmerLoop()
        {
            while (true)
            {
                shimmerPhase += Time.deltaTime * ShimmerFrequency;
                floodElapsed += Time.deltaTime;
                float alphaMultiplier = 1.0f + ShimmerAmplitude * Mathf.Sin(shimmerPhase * Mathf.PI * 2f);

                for (int i = 0; i < overlayRenderers.Count; i++)
                {
                    if (!activeOverlays[i].activeSelf) continue;
                    Color c = baseColors[i];
                    c.a *= alphaMultiplier * RevealFactor(i);
                    overlayRenderers[i].color = c;
                }

                yield return null;
            }
        }

        private float RevealFactor(int index)
        {
            if (floodDuration <= 0f) return 1f;
            float since = floodElapsed - revealDelays[index];
            return since <= 0f ? 0f : Mathf.Clamp01(since / TileFadeSeconds);
        }
    }
}
