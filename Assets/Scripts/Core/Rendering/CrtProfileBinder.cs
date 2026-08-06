using UnityEngine;

namespace ProjectAstra.Core.Rendering
{
    // Pushes a CrtProfile onto the CRT material every frame.
    //
    // Every frame rather than once, so dragging a slider in the Inspector while the game runs is
    // visible immediately — which is the entire point of having a profile asset. It is a handful
    // of SetFloat calls against a material that is already resident.
    //
    // The renderer feature reads the same material, so this is the only thing that needs to know
    // a profile exists.
    [DefaultExecutionOrder(-50)]
    public class CrtProfileBinder : MonoBehaviour
    {
        [Tooltip("The values to apply. Duplicate the asset to make a preset.")]
        [SerializeField] private CrtProfile profile;

        [Tooltip("Assets/Rendering/CRT/CRT.mat — the same material the renderer feature uses.")]
        [SerializeField] private Material crtMaterial;

        public static CrtProfileBinder Current { get; private set; }

        public CrtProfile Profile => profile;

        private static readonly int SourceResolutionId = Shader.PropertyToID("_SourceResolution");
        private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int BeamWidthId = Shader.PropertyToID("_BeamWidth");
        private static readonly int HorizontalBleedId = Shader.PropertyToID("_HorizontalBleed");
        private static readonly int GammaId = Shader.PropertyToID("_Gamma");
        private static readonly int GainId = Shader.PropertyToID("_Gain");

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void SetProfile(CrtProfile next)
        {
            profile = next;
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (profile == null || crtMaterial == null) return;

            crtMaterial.SetVector(SourceResolutionId,
                new Vector4(profile.SourceResolution.x, profile.SourceResolution.y, 0f, 0f));
            crtMaterial.SetFloat(ScanlineStrengthId, profile.ScanlineStrength);
            crtMaterial.SetFloat(BeamWidthId, profile.BeamWidth);
            crtMaterial.SetFloat(HorizontalBleedId, profile.HorizontalBleed);
            crtMaterial.SetFloat(GammaId, profile.Gamma);
            crtMaterial.SetFloat(GainId, profile.Gain);
        }
    }
}
