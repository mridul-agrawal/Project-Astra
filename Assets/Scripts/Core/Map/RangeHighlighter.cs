using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Manages programmatic sprite overlays for movement range, pass-through, and attack range.
    /// Creates and pools SpriteRenderers at runtime on the UIOverlay sorting layer.
    /// </summary>
    public class RangeHighlighter : MonoBehaviour
    {
        #region Fields
        private static readonly Color MovementColor = new(0.2f, 0.4f, 1.0f, 0.35f);
        private static readonly Color PassThroughColor = new(0.2f, 0.8f, 1.0f, 0.25f);
        private static readonly Color AttackColor = new(1.0f, 0.2f, 0.2f, 0.35f);

        private readonly List<GameObject> _activeOverlays = new();
        private readonly Queue<GameObject> _pool = new();
        private Sprite _overlaySprite;
        private Transform _overlayContainer;
        #endregion

        #region MonoBehaviour lifecycle
        private void Awake()
        {
            _overlaySprite = CreateOverlaySprite();
            _overlayContainer = new GameObject("RangeOverlays").transform;
        }

        private void OnDestroy()
        {
            if (_overlayContainer != null)
                Destroy(_overlayContainer.gameObject);
        }
        #endregion

        #region Public API
        public void ShowMovementRange(HashSet<Vector2Int> destinations, HashSet<Vector2Int> passThrough)
        {
            ClearAll();

            foreach (var tile in destinations)
                PlaceOverlay(tile, MovementColor);

            if (passThrough != null)
                foreach (var tile in passThrough)
                    PlaceOverlay(tile, PassThroughColor);
        }

        public void ShowAttackRange(HashSet<Vector2Int> attackable)
        {
            ClearAll();

            foreach (var tile in attackable)
                PlaceOverlay(tile, AttackColor);
        }

        public void ClearAll()
        {
            foreach (var overlay in _activeOverlays)
            {
                overlay.SetActive(false);
                _pool.Enqueue(overlay);
            }
            _activeOverlays.Clear();
        }
        #endregion

        #region Overlay placement and pooling
        private void PlaceOverlay(Vector2Int tile, Color color)
        {
            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            overlay.SetActive(true);

            var sr = overlay.GetComponent<SpriteRenderer>();
            sr.color = color;

            _activeOverlays.Add(overlay);
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
            sr.sortingOrder = -1; // Below cursor sprite (order 0)

            return overlay;
        }

        /// <summary>Creates a 16x16 white square sprite at runtime for tinting via SpriteRenderer.color.</summary>
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
        #endregion
    }
}
