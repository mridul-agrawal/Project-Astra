Shader "ProjectAstra/CRT"
{
    // Full-screen CRT filter for the battle map. Runs as a URP renderer feature, which means it
    // covers the game world and everything sitting in it (cursor, range overlays, HP bars) but
    // not the Screen Space Overlay HUD — that composites afterwards and stays crisp.
    //
    // Everything is driven from the source pixel grid rather than the output grid: the world is
    // authored at 480x270 and upscaled, so one "CRT pixel" is a block of output pixels and the
    // shader needs to know where that grid is to put a scanline in the right place.
    //
    // The boilerplate below is deliberately copied from URP's own CoreBlit.shader. The include
    // order matters (URP's Core.hlsl before Blit.hlsl, for the texture-array macros), and
    // editor_sync_compilation matters even more — without it Unity compiles this asynchronously
    // and quietly draws nothing for the first frames, which looks exactly like a broken effect.
    Properties
    {
        _SourceResolution ("Source Resolution", Vector) = (480, 270, 0, 0)
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35
    }

    HLSLINCLUDE
        #pragma target 2.0
        #pragma editor_sync_compilation

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _SourceResolution;
        float _ScanlineStrength;

        half4 FragCrt(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord;
            half4 colour = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

            // How far this output pixel sits from the centre of the source line it belongs to:
            // 0 in the middle of the line, 1 at the gap between lines.
            float offsetInLine = frac(uv.y * _SourceResolution.y) - 0.5;
            float distanceFromCentre = saturate(abs(offsetInLine) * 2.0);

            // Squared so the darkening stays near the edges instead of dimming the whole line —
            // a real scanline gap is narrow, not a general loss of brightness.
            float beam = 1.0 - _ScanlineStrength * distanceFromCentre * distanceFromCentre;

            colour.rgb *= beam;
            return colour;
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
