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
        // 480 / 32 PPU = 15 tiles wide. 480×270 is 16:9, upscaled 4× to 1920×1080.
        [Header("Pixel Perfect Settings")]
        [SerializeField] private int assetsPPU = 32;
        [SerializeField] private int referenceResolutionX = 480;
        [SerializeField] private int referenceResolutionY = 270;

        [Tooltip("Render the world into a 480x270 buffer and upscale it, instead of rendering at "
            + "window resolution with snapped sprites. Both look the same, but the buffer leaves "
            + "a full-screen pass only one pixel per source pixel to work with — which is not "
            + "enough room to draw a scanline gap or a phosphor mask. Leave this off for the CRT "
            + "filter to have anything to draw into.")]
        [SerializeField] private bool upscaleRenderTexture;

        private PixelPerfectCamera pixelPerfect;
        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;

            EnsurePixelPerfectComponent();
            ConfigurePixelPerfect();
        }

        private void OnValidate()
        {
            if (pixelPerfect != null)
                ConfigurePixelPerfect();
        }

        private void EnsurePixelPerfectComponent()
        {
            pixelPerfect = GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
                pixelPerfect = gameObject.AddComponent<PixelPerfectCamera>();
        }

        private void ConfigurePixelPerfect()
        {
            pixelPerfect.assetsPPU = assetsPPU;
            pixelPerfect.refResolutionX = referenceResolutionX;
            pixelPerfect.refResolutionY = referenceResolutionY;
            pixelPerfect.upscaleRT = upscaleRenderTexture;
            pixelPerfect.pixelSnapping = true;   // Snap sprites to the pixel grid — prevents blur.
        }
    }
}
