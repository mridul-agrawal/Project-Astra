using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Configures the camera for pixel-perfect tile rendering using URP's PixelPerfectCamera.
    /// Ensures integer-scale display (1x, 2x, 3x, 4x) with no sub-pixel artifacts, tile bleed,
    /// or gaps. Automatically handles window resize by recalculating the nearest valid scale.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCamera : MonoBehaviour
    {
        [Header("Pixel Perfect Settings")]
        [SerializeField] private int _assetsPPU = 16;             // Must match the tile size in pixels
        [SerializeField] private int _referenceResolutionX = 320; // Viewport width at 1x scale
        [SerializeField] private int _referenceResolutionY = 180; // Viewport height at 1x scale

        private PixelPerfectCamera _pixelPerfect;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;

            EnsurePixelPerfectComponent();
            ConfigurePixelPerfect();
        }

        private void EnsurePixelPerfectComponent()
        {
            _pixelPerfect = GetComponent<PixelPerfectCamera>();
            if (_pixelPerfect == null)
                _pixelPerfect = gameObject.AddComponent<PixelPerfectCamera>();
        }

        private void ConfigurePixelPerfect()
        {
            _pixelPerfect.assetsPPU = _assetsPPU;
            _pixelPerfect.refResolutionX = _referenceResolutionX;
            _pixelPerfect.refResolutionY = _referenceResolutionY;
            _pixelPerfect.upscaleRT = true;       // Render at reference resolution, then upscale
            _pixelPerfect.pixelSnapping = true;    // Snap sprites to pixel grid — prevents blurring
        }

        // Re-apply settings when values are changed in the Inspector
        private void OnValidate()
        {
            if (_pixelPerfect != null)
                ConfigurePixelPerfect();
        }
    }
}
