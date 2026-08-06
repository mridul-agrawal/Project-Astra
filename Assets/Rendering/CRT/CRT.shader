Shader "ProjectAstra/CRT"
{
    // Full-screen CRT filter for the battle map. Runs as a URP renderer feature, which means it
    // covers the game world and everything sitting in it (cursor, range overlays, HP bars) but
    // not the Screen Space Overlay HUD — that composites afterwards and stays crisp.
    //
    // The model, in the order a real tube does it: the beam sweeps horizontally and its light
    // spreads into neighbouring pixels as it goes; the tube's response to signal is a power law;
    // and the beam is only lit along discrete scanlines, with gaps between them. Horizontal is
    // continuous and soft, vertical is discrete and hard — that asymmetry is what makes the
    // result read as structured rather than merely blurry.
    //
    // The project renders in linear colour space, so the beam multiply is applied directly to
    // linear light, which is where it physically belongs.
    //
    // The boilerplate is copied from URP's own CoreBlit.shader. The include order matters, and
    // editor_sync_compilation matters more — without it Unity compiles this asynchronously and
    // quietly draws nothing for the first frames, which is indistinguishable from a broken effect.
    Properties
    {
        _SourceResolution ("Source Resolution", Vector) = (480, 270, 0, 0)
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35
        _BeamWidth ("Beam Width", Range(0.15, 1)) = 0.4
        _HorizontalBleed ("Horizontal Bleed", Range(0, 2)) = 0.4
        _Gamma ("Gamma", Range(1, 2.5)) = 1.1
        _Gain ("Gain", Range(0.5, 2.5)) = 1.2
    }

    HLSLINCLUDE
        #pragma target 2.0
        #pragma editor_sync_compilation

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _SourceResolution;
        float _ScanlineStrength;
        float _BeamWidth;
        float _HorizontalBleed;
        float _Gamma;
        float _Gain;

        // The analog sweep. Five taps across neighbouring source pixels, Gaussian weighted, so a
        // pixel's light bleeds sideways into its neighbours the way a moving beam's does. This is
        // what lets two dithered colours merge into a third the palette never held.
        half3 SweepHorizontally(float2 uv, float sourcePixelWidth)
        {
            float sigma = max(_HorizontalBleed, 0.0001);

            half3 sum = 0.0;
            float weightSum = 0.0;

            [unroll]
            for (int tap = -2; tap <= 2; tap++)
            {
                float distance = (float)tap;
                float weight = exp(-(distance * distance) / (2.0 * sigma * sigma));

                float2 tapUv = float2(uv.x + distance * sourcePixelWidth, uv.y);
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, tapUv).rgb * weight;
                weightSum += weight;
            }

            return sum / weightSum;
        }

        // Where this output pixel falls within its scanline, shaped as a Gaussian centred on the
        // line. Narrow beams leave dark gaps; wide ones fill the line until the lines vanish.
        float BeamProfile(float2 uv)
        {
            float offsetInLine = frac(uv.y * _SourceResolution.y) - 0.5;
            float sigma = max(_BeamWidth, 0.0001);
            float beam = exp(-(offsetInLine * offsetInLine) / (2.0 * sigma * sigma));

            return lerp(1.0, beam, _ScanlineStrength);
        }

        half4 FragCrt(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord;
            float sourcePixelWidth = 1.0 / max(_SourceResolution.x, 1.0);

            half3 colour = SweepHorizontally(uv, sourcePixelWidth);

            // Tone first, then the beam. The beam is a physical modulation of light, so it has to
            // land after the response curve rather than be shaped by it.
            colour = pow(max(colour, 0.0), _Gamma);
            colour *= BeamProfile(uv);
            colour *= _Gain;

            return half4(colour, 1.0);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CRT"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCrt
            ENDHLSL
        }
    }

    Fallback Off
}
