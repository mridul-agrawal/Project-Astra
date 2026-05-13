using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Grid;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core.Camera
{
    // Deadzone-based camera that follows the cursor across the battle map. When the cursor
    // pushes past the deadzone boundary on a frame, the camera scrolls by 1 tile in that
    // direction. Position is clamped to map bounds and integer-aligned so the
    // PixelPerfectCamera can render pixel-perfect without sub-pixel drift.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridCursor _gridCursor;
        [SerializeField] private MapRenderer _mapRenderer;

        [Header("Deadzone")]
        [SerializeField] private int _deadzoneMarginTiles = 3;

        // Top-left corner of the viewport, in tile coordinates.
        private Vector2Int _cameraGridPos;
        private int _viewportTilesW;
        private int _viewportTilesH;
        private bool _viewportInitialized;

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
            if (!_viewportInitialized) InitializeViewport();
            ApplyCameraPosition();
        }

        public void RecalculateViewport()
        {
            if (TryRecalculateFromPixelPerfectCamera()) return;
            RecalculateFromOrthographicCamera();
        }

        public void CenterOnTile(Vector2Int tile)
        {
            _cameraGridPos = new Vector2Int(
                tile.x - _viewportTilesW / 2,
                tile.y - _viewportTilesH / 2);

            ClampToMapBounds();
            ApplyCameraPosition();
        }

        // Init runs on the first LateUpdate because PixelPerfectCamera writes its viewport
        // values via a render callback that fires after Awake/Start — so we can't read the
        // correct dimensions any earlier.
        private void InitializeViewport()
        {
            RecalculateViewport();
            CenterOnTile(_gridCursor != null ? _gridCursor.GridPosition : Vector2Int.zero);
            _viewportInitialized = true;
        }

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

        // PixelPerfectCamera path: ref resolution and PPU are deterministic and set in
        // MapCamera.Awake, so we can trust them. Returns false when no PPC is present (or
        // it's misconfigured) so the caller can fall back to the orthographic-camera path.
        private bool TryRecalculateFromPixelPerfectCamera()
        {
            var ppc = GetComponent<PixelPerfectCamera>();
            if (ppc == null || ppc.assetsPPU <= 0) return false;

            _viewportTilesW = ppc.refResolutionX / ppc.assetsPPU;
            _viewportTilesH = ppc.refResolutionY / ppc.assetsPPU;
            return true;
        }

        private void RecalculateFromOrthographicCamera()
        {
            var cam = GetComponent<UnityEngine.Camera>();
            if (cam == null || !cam.orthographic) return;

            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            _viewportTilesW = Mathf.FloorToInt(worldWidth);
            _viewportTilesH = Mathf.FloorToInt(worldHeight);
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

        // Camera center = top-left tile + half-viewport. Odd viewport widths land on a
        // half-integer (7.5, etc.); PixelPerfectCamera snaps sprites to the pixel grid at
        // render time, so no world-unit floor is needed here.
        private void ApplyCameraPosition()
        {
            float centerX = _cameraGridPos.x + _viewportTilesW * 0.5f;
            float centerY = _cameraGridPos.y + _viewportTilesH * 0.5f;
            transform.position = new Vector3(centerX, centerY, transform.position.z);
        }

        #region Test helpers

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

        #endregion
    }
}
