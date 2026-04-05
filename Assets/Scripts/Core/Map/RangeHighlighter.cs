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
        private static readonly Color MovementColor = new(0.2f, 0.4f, 1.0f, 0.35f);
        private static readonly Color PassThroughColor = new(0.2f, 0.8f, 1.0f, 0.25f);
        private static readonly Color AttackColor = new(1.0f, 0.2f, 0.2f, 0.35f);

        private readonly List<GameObject> _activeOverlays = new();
        private readonly Queue<GameObject> _pool = new();
        private Sprite _overlaySprite;

        private void Awake()
        {
            _overlaySprite = CreateOverlaySprite();
        }

        public void ShowMovementRange(HashSet<Vector2Int> destinations, HashSet<Vector2Int> passThrough)
        {
            ClearAll();

            foreach (var tile in destinations)
                PlaceOverlay(tile, MovementColor);

            if (passThrough != null)
            {
                foreach (var tile in passThrough)
                    PlaceOverlay(tile, PassThroughColor);
            }
        }

        public void ShowAttackRange(HashSet<Vector2Int> attackable)
        {
            ClearAll();

            foreach (var tile in attackable)
                PlaceOverlay(tile, AttackColor);
        }

        public void ClearAll()
        {
            foreach (var go in _activeOverlays)
            {
                go.SetActive(false);
                _pool.Enqueue(go);
            }
            _activeOverlays.Clear();
        }

        private void PlaceOverlay(Vector2Int tile, Color color)
        {
            GameObject go = GetOrCreateOverlay();
            go.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            go.SetActive(true);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = color;

            _activeOverlays.Add(go);
        }

        private GameObject GetOrCreateOverlay()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var go = new GameObject("RangeOverlay");
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _overlaySprite;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = -1; // Below cursor sprite (order 0)

            return go;
        }

        /// <summary>Creates a 16×16 white square sprite at runtime for tinting via SpriteRenderer.color.</summary>
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
