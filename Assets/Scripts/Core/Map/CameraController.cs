using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core
{
    /// <summary>
    /// Deadzone-based camera tracking for the tactical battle map.
    /// Subscribes to GridCursor.OnCursorMoved and scrolls the camera by 1 tile
    /// when the cursor pushes past the deadzone boundary. Camera position is
    /// always clamped to map bounds and integer-snapped for pixel-perfect rendering.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridCursor _gridCursor;
        [SerializeField] private MapRenderer _mapRenderer;

        [Header("Deadzone")]
        [SerializeField] private int _deadzoneMarginTiles = 3;

        // Top-left corner of the viewport in tile coordinates
        private Vector2Int _cameraGridPos;
        private int _viewportTilesW;
        private int _viewportTilesH;
        private bool _viewportInitialized;

        /// <summary>Top-left corner of the viewport in world coordinates. Queryable by any system.</summary>
        public Vector2 WorldTopLeft => _cameraGridPos;

        public int ViewportTilesW => _viewportTilesW;
        public int ViewportTilesH => _viewportTilesH;
        public Vector2Int CameraGridPos => _cameraGridPos;

        private void OnEnable()
        {
            if (_gridCursor == null)
            {
                Debug.LogError($"{nameof(CameraController)} on '{name}' has no GridCursor assigned — camera will not pan. Wire the reference in the scene.", this);
                return;
            }
            _gridCursor.OnCursorMoved += OnCursorMoved;
        }

        private void OnDisable()
        {
            if (_gridCursor != null)
                _gridCursor.OnCursorMoved -= OnCursorMoved;
        }

        private void LateUpdate()
        {
            // Deferred init: PixelPerfectCamera updates orthographicSize via a render
            // callback after Awake/Start, so the first LateUpdate is the earliest point
            // we can read the correct viewport dimensions.
            if (!_viewportInitialized)
            {
                RecalculateViewport();
                CenterOnTile(_gridCursor != null ? _gridCursor.GridPosition : Vector2Int.zero);
                _viewportInitialized = true;
            }

            ApplyCameraPosition();
        }

        /// <summary>Recalculate viewport tile dimensions from camera settings.</summary>
        public void RecalculateViewport()
        {
            // Prefer PixelPerfectCamera ref resolution when present: it's set in MapCamera.Awake
            // and is deterministic, unlike camera.orthographicSize which PixelPerfectCamera only
            // updates during the render callback (after LateUpdate).
            var ppc = GetComponent<PixelPerfectCamera>();
            if (ppc != null && ppc.assetsPPU > 0)
            {
                _viewportTilesW = ppc.refResolutionX / ppc.assetsPPU;
                _viewportTilesH = ppc.refResolutionY / ppc.assetsPPU;
                return;
            }

            var cam = GetComponent<Camera>();
            if (cam == null || !cam.orthographic) return;

            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            _viewportTilesW = Mathf.FloorToInt(worldWidth);
            _viewportTilesH = Mathf.FloorToInt(worldHeight);
        }

        /// <summary>Instantly center the viewport on the given tile, clamped to map bounds.</summary>
        public void CenterOnTile(Vector2Int tile)
        {
            _cameraGridPos = new Vector2Int(
                tile.x - _viewportTilesW / 2,
                tile.y - _viewportTilesH / 2);

            ClampToMapBounds();
            ApplyCameraPosition();
        }

        // --- Core tracking logic ---

        internal void OnCursorMoved(Vector2Int cursorPos)
        {
            ScrollIfOutsideDeadzone(cursorPos);
        }

        internal void ScrollIfOutsideDeadzone(Vector2Int cursorPos)
        {
            int localX = cursorPos.x - _cameraGridPos.x;
            int localY = cursorPos.y - _cameraGridPos.y;

            int dzLeft = _deadzoneMarginTiles;
            int dzRight = _viewportTilesW - 1 - _deadzoneMarginTiles;
            int dzBottom = _deadzoneMarginTiles;
            int dzTop = _viewportTilesH - 1 - _deadzoneMarginTiles;

            if (localX < dzLeft) _cameraGridPos.x -= 1;
            else if (localX > dzRight) _cameraGridPos.x += 1;

            if (localY < dzBottom) _cameraGridPos.y -= 1;
            else if (localY > dzTop) _cameraGridPos.y += 1;

            ClampToMapBounds();
        }

        private void ClampToMapBounds()
        {
            MapData map = _mapRenderer != null ? _mapRenderer.CurrentMap : null;
            if (map == null) return;

            int maxX = Mathf.Max(0, map.Width - _viewportTilesW);
            int maxY = Mathf.Max(0, map.Height - _viewportTilesH);

            _cameraGridPos.x = Mathf.Clamp(_cameraGridPos.x, 0, maxX);
            _cameraGridPos.y = Mathf.Clamp(_cameraGridPos.y, 0, maxY);
        }

        private void ApplyCameraPosition()
        {
            // Camera center = top-left tile + half-viewport. For odd viewport widths this
            // lands on a half-integer (e.g. 7.5), which is correct — PixelPerfectCamera
            // snaps sprites to the pixel grid at render time, so no world-unit floor needed.
            float centerX = _cameraGridPos.x + _viewportTilesW * 0.5f;
            float centerY = _cameraGridPos.y + _viewportTilesH * 0.5f;

            transform.position = new Vector3(centerX, centerY, transform.position.z);
        }

        // --- Test helpers ---

        internal void Initialize(GridCursor cursor, MapRenderer mapRenderer, int deadzoneMargin,
            int viewportW, int viewportH)
        {
            _gridCursor = cursor;
            _mapRenderer = mapRenderer;
            _deadzoneMarginTiles = deadzoneMargin;
            _viewportTilesW = viewportW;
            _viewportTilesH = viewportH;
            _cameraGridPos = Vector2Int.zero;
        }

        internal void SetCameraGridPos(Vector2Int pos)
        {
            _cameraGridPos = pos;
        }
    }
}
