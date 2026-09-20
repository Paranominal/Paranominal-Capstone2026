Shader "Custom/URP/CRTShader"
{
    Properties
    {
        _BlurOffset ("Blur Offset", Range(0.0, 3.0)) = 1.0

        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 1.0)) = 0.3
        _ScanlineCount ("Scanline Count", Range(50, 1000)) = 300
        _ScanlineSpeed ("Scanline Speed", Range(0.0, 5.0)) = 0.5

        _VignetteIntensity ("Vignette Intensity", Range(0.0, 1.0)) = 0.3
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01, 1.0)) = 0.3

        _UsePhosphor ("Use Phosphor", Float) = 0
        _PhosphorIntensity ("Phosphor Intensity", Range(0.0, 1.0)) = 0.15
        _PhosphorScale ("Phosphor Scale", Range(1.0, 6.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "CRTFullscreenPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BlurOffset;
                float _ScanlineIntensity;
                float _ScanlineCount;
                float _ScanlineSpeed;
                float _VignetteIntensity;
                float _VignetteSmoothness;
                float _UsePhosphor;
                float _PhosphorIntensity;
                float _PhosphorScale;
            CBUFFER_END

            float _ResolutionScale; // EDIT (RenderResolutionManager): Global resolution scale (set by RenderResolutionManager).

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 texelSize = 1.0 / _ScreenParams.xy;

                // Soft blur: average centre with four directional samples.
                float2 offsetX = float2(texelSize.x * _BlurOffset, 0.0);
                float2 offsetY = float2(0.0, texelSize.y * _BlurOffset);

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offsetX);
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offsetX);
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offsetY);
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offsetY);
                col /= 5.0;

                // Scrolling scanlines.
                float scanlinePhase = uv.y * _ScanlineCount + _Time.y * _ScanlineSpeed;
                float rawScanline = sin(scanlinePhase * 3.14159) * 0.5 + 0.5;
                float scanline = lerp(1.0, rawScanline, _ScanlineIntensity);
                col.rgb *= scanline;

                // Phosphor pattern, tied to lit scanline rows.
                if (_UsePhosphor > 0.5 && _PhosphorIntensity > 0.001)
                {
                    float2 screenPos = uv * _ScreenParams.xy;
                    int pixel = (int)(screenPos.x / (_PhosphorScale * _ResolutionScale)) % 3; // EDIT (RenderResolutionManager): Scale phosphor columns with resolution.

                    float dim = 1.0 - _PhosphorIntensity;
                    float3 mask = float3(1, 1, 1);

                    if (pixel == 0)
                        mask = float3(1.0, dim, dim);
                    else if (pixel == 1)
                        mask = float3(dim, 1.0, dim);
                    else
                        mask = float3(dim, dim, 1.0);

                    // Fade phosphors out in dark scanline gaps.
                    mask = lerp(float3(1, 1, 1), mask, rawScanline);
                    col.rgb *= mask;
                }

                // Vignette.
                float2 vignetteUV = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(vignetteUV, vignetteUV) * _VignetteIntensity;
                vignette = smoothstep(0.0, _VignetteSmoothness, vignette);
                col.rgb *= saturate(vignette);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
