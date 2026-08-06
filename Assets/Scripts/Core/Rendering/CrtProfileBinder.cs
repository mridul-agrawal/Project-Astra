using UnityEngine;

namespace ProjectAstra.Core.Rendering
{
    // Picks the profile the player's setting asks for and pushes it onto the CRT material.
    //
    // Every frame rather than once, so dragging a slider in the Inspector while the game runs is
    // visible immediately — which is the entire point of having a profile asset. It is a handful
    // of SetFloat calls against a material that is already resident.
    //
    // The renderer feature reads the same material, so this is the only thing in the game that
    // needs to know a profile exists.
    [DefaultExecutionOrder(-50)]
    public class CrtProfileBinder : MonoBehaviour
    {
        [Tooltip("Assets/Rendering/CRT/CRT.mat — the same material the renderer feature uses.")]
        [SerializeField] private Material crtMaterial;

        [Tooltip("The player's persisted Off / Subtle / Full preference.")]
        [SerializeField] private CrtSettings settings;

        [Tooltip("Used when the player picks Subtle. A restrained tuning meant to read as a warm "
            + "screen rather than a filter.")]
        [SerializeField] private CrtProfile subtleProfile;

        [Tooltip("Used when the player picks Full. Everything turned up to where the effect is "
            + "unmistakably a CRT.")]
        [SerializeField] private CrtProfile fullProfile;

        public static CrtProfileBinder Current { get; private set; }

        // Whichever profile the current setting selects — this is what the F2 tuning overlay
        // edits, so tweaks always land on the preset you can actually see.
        public CrtProfile ActiveProfile =>
            Quality == CrtQuality.Full ? fullProfile : subtleProfile;

        public CrtQuality Quality
        {
            get => settings != null ? settings.Persisted : CrtQuality.Off;
            set { if (settings != null) settings.Persisted = value; }
        }

        private static readonly int EnabledId = Shader.PropertyToID("_Enabled");
        private static readonly int SourceResolutionId = Shader.PropertyToID("_SourceResolution");
        private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int BeamWidthId = Shader.PropertyToID("_BeamWidth");
        private static readonly int HorizontalBleedId = Shader.PropertyToID("_HorizontalBleed");
        private static readonly int GammaId = Shader.PropertyToID("_Gamma");
        private static readonly int GainId = Shader.PropertyToID("_Gain");
        private static readonly int BloomAmountId = Shader.PropertyToID("_BloomAmount");
        private static readonly int HalationStrengthId = Shader.PropertyToID("_HalationStrength");
        private static readonly int HalationRadiusId = Shader.PropertyToID("_HalationRadius");
        private static readonly int HalationThresholdId = Shader.PropertyToID("_HalationThreshold");
        private static readonly int HalationTintId = Shader.PropertyToID("_HalationTint");
        private static readonly int MaskTypeId = Shader.PropertyToID("_MaskType");
        private static readonly int MaskStrengthId = Shader.PropertyToID("_MaskStrength");
        private static readonly int MaskPitchId = Shader.PropertyToID("_MaskPitch");
        private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");
        private static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (crtMaterial == null) return;

            // Off leaves the pass running but passing the image straight through. The cost is one
            // fullscreen sample, which is cheaper than reaching into the renderer data at runtime
            // to disable the feature and far harder to get wrong.
            if (Quality == CrtQuality.Off)
            {
                crtMaterial.SetFloat(EnabledId, 0f);
                return;
            }

            var profile = ActiveProfile;
            if (profile == null)
            {
                crtMaterial.SetFloat(EnabledId, 0f);
                return;
            }

            crtMaterial.SetFloat(EnabledId, 1f);
            crtMaterial.SetVector(SourceResolutionId,
                new Vector4(profile.SourceResolution.x, profile.SourceResolution.y, 0f, 0f));
            crtMaterial.SetFloat(ScanlineStrengthId, profile.ScanlineStrength);
            crtMaterial.SetFloat(BeamWidthId, profile.BeamWidth);
            crtMaterial.SetFloat(HorizontalBleedId, profile.HorizontalBleed);
            crtMaterial.SetFloat(GammaId, profile.Gamma);
            crtMaterial.SetFloat(GainId, profile.Gain);
            crtMaterial.SetFloat(BloomAmountId, profile.BloomAmount);
            crtMaterial.SetFloat(HalationStrengthId, profile.HalationStrength);
            crtMaterial.SetFloat(HalationRadiusId, profile.HalationRadius);
            crtMaterial.SetFloat(HalationThresholdId, profile.HalationThreshold);
            crtMaterial.SetColor(HalationTintId, profile.HalationTint);
            crtMaterial.SetFloat(MaskTypeId, (float)profile.MaskType);
            crtMaterial.SetFloat(MaskStrengthId, profile.MaskStrength);
            crtMaterial.SetFloat(MaskPitchId, profile.MaskPitch);
            crtMaterial.SetFloat(CurvatureId, profile.Curvature);
            crtMaterial.SetFloat(VignetteStrengthId, profile.VignetteStrength);
        }
    }
}
