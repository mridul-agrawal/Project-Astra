using System.Collections;
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
        [SerializeField] private GridCursor gridCursor;
        [SerializeField] private MapRenderer mapRenderer;

        [Header("Deadzone")]
        [SerializeField] private int deadzoneMarginTiles = 3;

        // Top-left corner of the viewport, in tile coordinates.
        private Vector2Int cameraGridPos;
        private int viewportTilesW;
        private int viewportTilesH;
        private bool viewportInitialized;

        // While a scripted pan drives the transform directly (e.g. a cinematic), LateUpdate must
        // not stomp it back to the grid-aligned cursor-follow position.
        private bool externalControl;

        private UnityEngine.Camera cam;
        private PixelPerfectCamera ppc;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            ppc = GetComponent<PixelPerfectCamera>();
        }

        public Vector2 WorldTopLeft => cameraGridPos;
        public int ViewportTilesW => viewportTilesW;
        public int ViewportTilesH => viewportTilesH;
        public Vector2Int CameraGridPos => cameraGridPos;

        private void OnEnable()
        {
            if (gridCursor == null)
            {
                Debug.LogError($"{nameof(CameraController)} on '{name}' has no GridCursor assigned — camera will not pan. Wire the reference in the scene.", this);
                return;
            }
            gridCursor.OnCursorMoved += OnCursorMoved;
        }

        private void OnDisable()
        {
            if (gridCursor != null)
                gridCursor.OnCursorMoved -= OnCursorMoved;
        }

        private void LateUpdate()
        {
            if (!viewportInitialized) InitializeViewport();
            if (externalControl) return;
            ApplyCameraPosition();
        }

        // Lets a cinematic frame the map before the first LateUpdate runs, without the usual
        // "center on the cursor" that InitializeViewport does.
        public void EnsureViewportReady()
        {
            if (viewportInitialized) return;
            RecalculateViewport();
            viewportInitialized = true;
        }

        // --- Cinematic control (scripted sequences like Beat 0) ---
        // The PixelPerfectCamera is turned off for the duration so we can zoom/pan smoothly;
        // LateUpdate leaves the transform alone (_externalControl). EndCinematic restores
        // pixel-perfect gameplay framing.

        public void BeginCinematic()
        {
            EnsureViewportReady();
            externalControl = true;
            if (ppc != null) ppc.enabled = false;
        }

        // Snap the cinematic framing instantly — used to set the opening shot.
        public void SnapCinematicFrame(Vector2Int centerTile, float orthographicSize)
        {
            transform.position = WorldCenterOf(centerTile);
            if (cam != null) cam.orthographicSize = orthographicSize;
        }

        // Lerp framing (position + zoom) to a new shot over duration.
        public IEnumerator CinematicFrameTo(Vector2Int centerTile, float orthographicSize, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = WorldCenterOf(centerTile);
            float startOrtho = cam != null ? cam.orthographicSize : orthographicSize;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPos, endPos, t);
                if (cam != null) cam.orthographicSize = Mathf.Lerp(startOrtho, orthographicSize, t);
                yield return null;
            }
            transform.position = endPos;
            if (cam != null) cam.orthographicSize = orthographicSize;
        }

        // Hand control back to gameplay: pixel-perfect on, cursor-follow resumes framed here.
        public void EndCinematic(Vector2Int restoreCenterTile)
        {
            if (ppc != null) ppc.enabled = true;   // restores the pixel-perfect ortho size
            cameraGridPos = ClampedGridPosFor(restoreCenterTile);
            externalControl = false;
        }

        private Vector3 WorldCenterOf(Vector2Int tile) =>
            new(tile.x + 0.5f, tile.y + 0.5f, transform.position.z);

        // A quick decaying camera shake around the current position. Only meaningful under
        // cinematic control (otherwise LateUpdate immediately resets the transform).
        public IEnumerator Shake(float magnitude, float duration)
        {
            Vector3 basePos = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (magnitude * damper);
                transform.position = new Vector3(basePos.x + offset.x, basePos.y + offset.y, basePos.z);
                yield return null;
            }
            transform.position = basePos;
        }

        public void RecalculateViewport()
        {
            if (TryRecalculateFromPixelPerfectCamera()) return;
            RecalculateFromOrthographicCamera();
        }

        public void CenterOnTile(Vector2Int tile)
        {
            cameraGridPos = ClampedGridPosFor(tile);
            ApplyCameraPosition();
        }

        // Viewport top-left that centers on a tile, clamped to the map.
        private Vector2Int ClampedGridPosFor(Vector2Int tile)
        {
            var pos = new Vector2Int(tile.x - viewportTilesW / 2, tile.y - viewportTilesH / 2);
            MapData map = mapRenderer != null ? mapRenderer.CurrentMap : null;
            if (map != null)
            {
                pos.x = Mathf.Clamp(pos.x, 0, Mathf.Max(0, map.Width - viewportTilesW));
                pos.y = Mathf.Clamp(pos.y, 0, Mathf.Max(0, map.Height - viewportTilesH));
            }
            return pos;
        }

        // Init runs on the first LateUpdate because PixelPerfectCamera writes its viewport
        // values via a render callback that fires after Awake/Start — so we can't read the
        // correct dimensions any earlier.
        private void InitializeViewport()
        {
            RecalculateViewport();
            CenterOnTile(gridCursor != null ? gridCursor.GridPosition : Vector2Int.zero);
            viewportInitialized = true;
        }

        internal void OnCursorMoved(Vector2Int cursorPos)
        {
            ScrollIfOutsideDeadzone(cursorPos);
        }

        internal void ScrollIfOutsideDeadzone(Vector2Int cursorPos)
        {
            int localX = cursorPos.x - cameraGridPos.x;
            int localY = cursorPos.y - cameraGridPos.y;

            cameraGridPos.x += DeadzoneScrollDelta(localX, viewportTilesW);
            cameraGridPos.y += DeadzoneScrollDelta(localY, viewportTilesH);

            ClampToMapBounds();
        }

        // -1 / 0 / +1: scroll one tile when the cursor sits inside the near or far
        // deadzone margin on this axis, otherwise hold.
        private int DeadzoneScrollDelta(int local, int viewportTiles)
        {
            int near = deadzoneMarginTiles;
            int far = viewportTiles - 1 - deadzoneMarginTiles;

            if (local < near) return -1;
            if (local > far) return 1;
            return 0;
        }

        // PixelPerfectCamera path: ref resolution and PPU are deterministic and set in
        // MapCamera.Awake, so we can trust them. Returns false when no PPC is present (or
        // it's misconfigured) so the caller can fall back to the orthographic-camera path.
        private bool TryRecalculateFromPixelPerfectCamera()
        {
            var ppc = GetComponent<PixelPerfectCamera>();
            if (ppc == null || ppc.assetsPPU <= 0) return false;

            viewportTilesW = ppc.refResolutionX / ppc.assetsPPU;
            viewportTilesH = ppc.refResolutionY / ppc.assetsPPU;
            return true;
        }

        private void RecalculateFromOrthographicCamera()
        {
            var cam = GetComponent<UnityEngine.Camera>();
            if (cam == null || !cam.orthographic) return;

            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            viewportTilesW = Mathf.FloorToInt(worldWidth);
            viewportTilesH = Mathf.FloorToInt(worldHeight);
        }

        private void ClampToMapBounds()
        {
            MapData map = mapRenderer != null ? mapRenderer.CurrentMap : null;
            if (map == null) return;

            int maxX = Mathf.Max(0, map.Width - viewportTilesW);
            int maxY = Mathf.Max(0, map.Height - viewportTilesH);

            cameraGridPos.x = Mathf.Clamp(cameraGridPos.x, 0, maxX);
            cameraGridPos.y = Mathf.Clamp(cameraGridPos.y, 0, maxY);
        }

        // Camera center = top-left tile + half-viewport. Odd viewport widths land on a
        // half-integer (7.5, etc.); PixelPerfectCamera snaps sprites to the pixel grid at
        // render time, so no world-unit floor is needed here.
        private void ApplyCameraPosition()
        {
            float centerX = cameraGridPos.x + viewportTilesW * 0.5f;
            float centerY = cameraGridPos.y + viewportTilesH * 0.5f;
            transform.position = new Vector3(centerX, centerY, transform.position.z);
        }

        #region Test helpers

        internal void Initialize(GridCursor cursor, MapRenderer mapRenderer, int deadzoneMargin,
            int viewportW, int viewportH)
        {
            gridCursor = cursor;
            this.mapRenderer = mapRenderer;
            deadzoneMarginTiles = deadzoneMargin;
            viewportTilesW = viewportW;
            viewportTilesH = viewportH;
            cameraGridPos = Vector2Int.zero;
        }

        internal void SetCameraGridPos(Vector2Int pos)
        {
            cameraGridPos = pos;
        }

        #endregion
    }
}
