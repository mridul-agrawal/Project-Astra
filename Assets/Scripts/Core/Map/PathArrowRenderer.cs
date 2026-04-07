using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Renders the optimal path from a unit's origin to the cursor tile as a visible
    /// overlay. Uses pooled SpriteRenderers on the UIOverlay sorting layer, same pattern
    /// as RangeHighlighter. Color: semi-transparent yellow to distinguish from range highlights.
    /// </summary>
    public class PathArrowRenderer : MonoBehaviour
    {
        private static readonly Color PathColor = new(1.0f, 1.0f, 0.3f, 0.5f);

        private readonly List<GameObject> _activeOverlays = new();
        private readonly Queue<GameObject> _pool = new();
        private Sprite _overlaySprite;
        private Transform _overlayContainer;

        private void Awake()
        {
            _overlaySprite = CreateOverlaySprite();
            _overlayContainer = new GameObject("PathOverlays").transform;
        }

        private void OnDestroy()
        {
            if (_overlayContainer != null)
                Destroy(_overlayContainer.gameObject);
        }

        /// <summary>Shows path overlay tiles along the given path. Skips the origin tile (index 0).</summary>
        public void ShowPath(List<Vector2Int> path)
        {
            Clear();

            if (!HasValidPath(path)) return;

            for (int i = 1; i < path.Count; i++)
                PlaceOverlay(path[i]);
        }

        public void Clear()
        {
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
            sr.sortingOrder = -2; // Below cursor (-0) and range highlights (-1)

            return overlay;
        }

        private static Sprite CreateOverlaySprite()
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color32[size * size];
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = white;

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
