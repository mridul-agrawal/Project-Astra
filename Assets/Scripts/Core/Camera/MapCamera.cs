using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectAstra.Core.Camera
{
    // Configures the scene camera for pixel-perfect tile rendering via URP's PixelPerfectCamera.
    // Locks display to integer-scale (1x, 2x, 3x, 4x) so no sub-pixel artifacts, tile bleed, or
    // gaps appear. PixelPerfectCamera handles window-resize rescaling automatically.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class MapCamera : MonoBehaviour
    {
        // 240 / 16 PPU = 15 tiles wide. 240×135 is 16:9, upscaled 8× to 1920×1080.
        [Header("Pixel Perfect Settings")]
        [SerializeField] private int _assetsPPU = 16;
        [SerializeField] private int _referenceResolutionX = 240;
        [SerializeField] private int _referenceResolutionY = 135;

        private PixelPerfectCamera _pixelPerfect;
        private UnityEngine.Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _camera.orthographic = true;

            EnsurePixelPerfectComponent();
            ConfigurePixelPerfect();
        }

        private void OnValidate()
        {
            if (_pixelPerfect != null)
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
            _pixelPerfect.upscaleRT = true;       // Render at ref resolution, then upscale to window.
            _pixelPerfect.pixelSnapping = true;   // Snap sprites to the pixel grid — prevents blur.
        }
    }
}
