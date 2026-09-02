using UnityEngine;
using ProjectAstra.Core.Camera;

namespace ProjectAstra.Core.Hub
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
    [RequireComponent(typeof(MapCamera))]
    public sealed class HubCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Tooltip("Raises the camera off the target's feet so she sits centred rather than low.")]
        [SerializeField] private float targetHeightOffset = 0.5f;

        private MapCamera mapCamera;

        public void Follow(Transform newTarget) => target = newTarget;

        // Who the camera is on, so a caller that borrows it can put it back.
        public Transform Target => target;

        // What the camera can currently see, for working out whether something is off-screen and
        // which edge to point at it from.
        public Vector2 Centre => transform.position;
        public Vector2 ViewSizeTiles => MapCameraOrNull != null ? MapCameraOrNull.ViewSizeTiles : Vector2.zero;

        private MapCamera MapCameraOrNull => mapCamera != null ? mapCamera : mapCamera = GetComponent<MapCamera>();

        private void LateUpdate()
        {
            if (target == null || HubLocationService.Instance == null) return;

            var desired = new Vector2(target.position.x, target.position.y + targetHeightOffset);
            Vector2 contained = CameraContainment.Contain(
                desired, HubLocationService.Instance.Bounds, ViewSizeTiles);

            // Rounded after containing, so the room's edge lands on a whole pixel too.
            Vector2 snapped = CameraContainment.RoundToPixel(contained, MapCameraOrNull.PixelsPerUnit);
            transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);
        }
    }
}
