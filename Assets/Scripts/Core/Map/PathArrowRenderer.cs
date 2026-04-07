using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public class PathArrowRenderer : MonoBehaviour
    {
        private static readonly Color PathColor = new(1.0f, 1.0f, 0.2f, 0.7f);

        const float ShimmerFrequency = 1.5f;
        const float ShimmerAmplitude = 0.15f;

        private readonly List<GameObject> _activeOverlays = new();
        private readonly Queue<GameObject> _pool = new();
        private Sprite _overlaySprite;
        private Transform _overlayContainer;
        private Coroutine _shimmerCoroutine;
        private float _shimmerPhase;

        private void Awake()
        {
            _overlaySprite = OverlaySpriteFactory.GetOverlaySprite();
            _overlayContainer = new GameObject("PathOverlays").transform;
        }

        private void OnDestroy()
        {
            if (_overlayContainer != null)
                Destroy(_overlayContainer.gameObject);
        }

        public void ShowPath(List<Vector2Int> path)
        {
            Clear();

            if (!HasValidPath(path)) return;

            for (int i = 1; i < path.Count; i++)
                PlaceOverlay(path[i]);

            StartShimmer();
        }

        public void Clear()
        {
            StopShimmer();

            foreach (var overlay in _activeOverlays)
            {
                overlay.SetActive(false);
                _pool.Enqueue(overlay);
            }
            _activeOverlays.Clear();
        }

        private bool HasValidPath(List<Vector2Int> path)
        {
            return path != null && path.Count > 1;
        }

        private void PlaceOverlay(Vector2Int tile)
        {
            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            overlay.SetActive(true);
            _activeOverlays.Add(overlay);
        }

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

                Color c = PathColor;
                c.a *= alphaMultiplier;

                foreach (var overlay in _activeOverlays)
                {
                    if (!overlay.activeSelf) continue;
                    overlay.GetComponent<SpriteRenderer>().color = c;
                }

                yield return null;
            }
        }

        private GameObject GetOrCreateOverlay()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var overlay = new GameObject("PathOverlay");
            overlay.transform.SetParent(_overlayContainer);

            var sr = overlay.AddComponent<SpriteRenderer>();
            sr.sprite = _overlaySprite;
            sr.color = PathColor;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = -2;

            return overlay;
        }
    }
}
