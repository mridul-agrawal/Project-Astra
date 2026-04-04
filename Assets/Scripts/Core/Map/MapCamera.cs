using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectAstra.Core
{
    [RequireComponent(typeof(Camera))]
    public class MapCamera : MonoBehaviour
    {
        [Header("Pixel Perfect Settings")]
        [SerializeField] private int _assetsPPU = 16;
        [SerializeField] private int _referenceResolutionX = 320;
        [SerializeField] private int _referenceResolutionY = 180;

        private PixelPerfectCamera _pixelPerfect;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;

            _pixelPerfect = GetComponent<PixelPerfectCamera>();
            if (_pixelPerfect == null)
                _pixelPerfect = gameObject.AddComponent<PixelPerfectCamera>();

            ConfigurePixelPerfect();
        }

        private void ConfigurePixelPerfect()
        {
            _pixelPerfect.assetsPPU = _assetsPPU;
            _pixelPerfect.refResolutionX = _referenceResolutionX;
            _pixelPerfect.refResolutionY = _referenceResolutionY;
            _pixelPerfect.upscaleRT = true;
            _pixelPerfect.pixelSnapping = true;
        }

        private void OnValidate()
        {
            if (_pixelPerfect != null)
                ConfigurePixelPerfect();
        }
    }
}
