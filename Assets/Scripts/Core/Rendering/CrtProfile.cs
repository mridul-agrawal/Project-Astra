using UnityEngine;

namespace ProjectAstra.Core.Rendering
{
    // Every knob the CRT filter has. Duplicate this asset to make a preset; nothing about the
    // look is hardcoded in the shader.
    //
    // The numbers describe a real display rather than a stack of effects: a beam that sweeps
    // horizontally and blends as it goes, discrete scanlines that don't, and a power-law
    // response. Tuning them is meant to feel like adjusting a television, not a filter.
    [CreateAssetMenu(fileName = "CrtProfile", menuName = "Project Astra/Rendering/CRT Profile")]
    public class CrtProfile : ScriptableObject
    {
        [Header("Source grid")]
        [Tooltip("The resolution the world is drawn at, which is where the scanlines are placed. "
            + "Must match MapCamera's reference resolution or the lines will drift against the "
            + "art instead of sitting on it.")]
        [SerializeField] private Vector2Int sourceResolution = new(480, 270);

        [Header("Beam")]
        [Tooltip("How dark the gap between scanlines goes. 0 is off. This is the single most "
            + "recognisable part of the effect, and the easiest to overdo.")]
        [Range(0f, 1f)][SerializeField] private float scanlineStrength = 0.35f;

        [Tooltip("How wide the lit part of each scanline is, as a fraction of the line's height. "
            + "Low values give thin bright lines with black between them; high values fill the "
            + "line in until the scanlines disappear.")]
        [Range(0.15f, 1f)][SerializeField] private float beamWidth = 0.4f;

        [Tooltip("How far a pixel's light spreads sideways into its neighbours, in source pixels. "
            + "This is the analog sweep — it is what lets two dithered colours merge into a third "
            + "that the palette never contained. 0 keeps pixels perfectly crisp.")]
        [Range(0f, 2f)][SerializeField] private float horizontalBleed = 0.4f;

        [Header("Response")]
        [Tooltip("Power-law response, applied in linear light. Above 1 deepens the shadows and "
            + "firms up contrast the way a real tube does; 1 leaves tone untouched.")]
        [Range(1f, 2.5f)][SerializeField] private float gamma = 1.1f;

        [Tooltip("Overall brightness. Scanlines eat light — roughly 15% at the default strength — "
            + "so this earns its keep putting it back. Real sets were driven hard for the same "
            + "reason.")]
        [Range(0.5f, 2.5f)][SerializeField] private float gain = 1.2f;

        public Vector2Int SourceResolution => sourceResolution;
        public float ScanlineStrength => scanlineStrength;
        public float BeamWidth => beamWidth;
        public float HorizontalBleed => horizontalBleed;
        public float Gamma => gamma;
        public float Gain => gain;
    }
}
