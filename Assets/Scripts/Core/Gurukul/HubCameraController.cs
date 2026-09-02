using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectAstra.Core.Gurukul
{
    // Follows the protagonist and never shows past the edges of the room she is in.
    //
    // Three rules keep this from shimmering, all of them consequences of the pixel-perfect camera
    // being set to snap sprites to the pixel grid:
    //
    //   1. Her position is never rounded. At 112 px/s she covers under two pixels a frame, so
    //      rounding the character would stutter. Snapping is the camera's job, not hers.
    //   2. The camera IS rounded, to whole pixels. If it sat on a fraction while sprites snapped,
    //      different sprites would round different ways and the whole scene would crawl.
    //   3. The follow is hard, with no smoothing. A lerped camera quantises out of phase with the
    //      character, which reads as her jittering inside the frame. Movement here is constant
    //      speed and cardinal, so there is nothing for smoothing to smooth.
    //
    // The hub's counterpart to CameraController, which does the same job for the battle map but
    // from an entirely different input: that one is welded to the grid cursor, stores its position
    // as a Vector2Int, and scrolls a whole tile at a time. Nothing here quantises to tiles.
    [DefaultExecutionOrder(100)]
    public sealed class HubCameraController : MonoBehaviour
    {
        private const float FallbackViewWidthTiles = 480f / 32f;
        private const float FallbackViewHeightTiles = 270f / 32f;
        private const int FallbackPixelsPerUnit = 32;

        [SerializeField] private Transform target;

        [Tooltip("Raises the camera off the target's feet so she sits centred rather than low.")]
        [SerializeField] private float targetHeightOffset = 0.5f;

        private PixelPerfectCamera pixelPerfect;
        private float viewWidthTiles = FallbackViewWidthTiles;
        private float viewHeightTiles = FallbackViewHeightTiles;
        private int pixelsPerUnit = FallbackPixelsPerUnit;

        public void Follow(Transform newTarget) => target = newTarget;

        // What the camera can currently see, for working out whether something is off-screen and
        // which edge to point at it from.
        public Vector2 Centre => transform.position;
        public Vector2 ViewSizeTiles => new(viewWidthTiles, viewHeightTiles);

        private void Awake()
        {
            pixelPerfect = GetComponent<PixelPerfectCamera>();
            ReadViewportFromCamera();
        }

        private void LateUpdate()
        {
            if (target == null || GurukulLocationService.Instance == null) return;

            var desired = new Vector2(target.position.x, target.position.y + targetHeightOffset);
            Vector2 contained = ContainWithinRoom(desired);

            transform.position = new Vector3(
                RoundToPixel(contained.x), RoundToPixel(contained.y), transform.position.z);
        }

        // The pixel-perfect component is added at runtime by MapCamera, so read it late rather than
        // requiring it in the inspector.
        private void ReadViewportFromCamera()
        {
            if (pixelPerfect == null) pixelPerfect = GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null || pixelPerfect.assetsPPU <= 0) return;

            pixelsPerUnit = pixelPerfect.assetsPPU;
            viewWidthTiles = pixelPerfect.refResolutionX / (float)pixelsPerUnit;
            viewHeightTiles = pixelPerfect.refResolutionY / (float)pixelsPerUnit;
        }

        // A room narrower than the view is centred instead of clamped, which would otherwise ask for
        // a minimum above the maximum.
        private Vector2 ContainWithinRoom(Vector2 desired)
        {
            Rect room = GurukulLocationService.Instance.Bounds;
            return new Vector2(
                ContainAxis(desired.x, room.xMin, room.xMax, viewWidthTiles),
                ContainAxis(desired.y, room.yMin, room.yMax, viewHeightTiles));
        }

        private static float ContainAxis(float desired, float min, float max, float viewSize)
        {
            float half = viewSize * 0.5f;
            if (max - min <= viewSize) return (min + max) * 0.5f;
            return Mathf.Clamp(desired, min + half, max - half);
        }

        // Rounded after clamping, so the room's edge lands on a whole pixel too.
        private float RoundToPixel(float value) => Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;
    }
}
