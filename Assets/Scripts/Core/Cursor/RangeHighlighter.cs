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

        private readonly List<GameObject> _activeOverlays = new();
        private readonly List<Color> _baseColors = new();
        private readonly Queue<GameObject> _pool = new();
        private Sprite _overlaySprite;
        private Transform _overlayContainer;
        private Coroutine _shimmerCoroutine;
        private float _shimmerPhase;

        private void Awake()
        {
            _overlaySprite = OverlaySpriteFactory.GetOverlaySprite();
            _overlayContainer = new GameObject("RangeOverlays").transform;
        }

        private void OnDestroy()
        {
            if (_overlayContainer != null)
                Destroy(_overlayContainer.gameObject);
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

            foreach (var overlay in _activeOverlays)
            {
                overlay.SetActive(false);
                _pool.Enqueue(overlay);
            }
            _activeOverlays.Clear();
            _baseColors.Clear();
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

            _activeOverlays.Add(overlay);
            _baseColors.Add(color);
        }

        private GameObject GetOrCreateOverlay()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var overlay = new GameObject("RangeOverlay");
            overlay.transform.SetParent(_overlayContainer);

            var sr = overlay.AddComponent<SpriteRenderer>();
            sr.sprite = _overlaySprite;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = -1;

            return overlay;
        }

        // --- Shimmer pulse ---

        private void StartShimmer()
        {
            if (_shimmerCoroutine == null)
                _shimmerCoroutine = StartCoroutine(ShimmerLoop());
        }

        private void StopShimmer()
        {
            if (_shimmerCoroutine != null)
            {
                StopCoroutine(_shimmerCoroutine);
                _shimmerCoroutine = null;
            }
        }

        private IEnumerator ShimmerLoop()
        {
            while (true)
            {
                _shimmerPhase += Time.deltaTime * ShimmerFrequency;
                float alphaMultiplier = 1.0f + ShimmerAmplitude * Mathf.Sin(_shimmerPhase * Mathf.PI * 2f);

                for (int i = 0; i < _activeOverlays.Count; i++)
                {
                    if (!_activeOverlays[i].activeSelf) continue;
                    var sr = _activeOverlays[i].GetComponent<SpriteRenderer>();
                    Color c = _baseColors[i];
                    c.a *= alphaMultiplier;
                    sr.color = c;
                }

                yield return null;
            }
        }
    }
}
