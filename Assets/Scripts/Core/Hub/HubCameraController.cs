using UnityEngine;
using ProjectAstra.Core.Camera;

namespace ProjectAstra.Core.Hub
{
    // Follows the protagonist and never shows past the edges of the room she is in.
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

        // A hard follow, and only the camera is rounded to whole pixels. Smoothing it, or rounding
        // her as well, makes the two quantise out of step and the whole scene crawls.
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
