Shader "Custom/URP/CRTShader"
{
    Properties
    {
        _Curvature ("Curvature", Range(1.0, 10.0)) = 1.0
        _VignetteWidth ("Vignette Width", Range(1.0, 100.0)) = 30.0
        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 1.0)) = 0.3
        _ScanlineCount ("Scanline Count", Range(50, 1000)) = 300
        _CornerRadius ("Corner Radius", Range(0.0, 0.2)) = 0.05
        _CornerSharpness ("Corner Sharpness", Range(1.0, 100.0)) = 20.0
        _PhosphorIntensity ("Phosphor Intensity", Range(0.0, 1.0)) = 0.15
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
                float _Curvature; // Barrel distortion strength.
                float _VignetteWidth; // Width of the darkened edge falloff.
                float _ScanlineIntensity; // How dark the gaps between scanline rows are.
                float _ScanlineCount; // Number of scanlines across the screen height.
                float _CornerRadius; // How rounded the screen corners are.
                float _CornerSharpness; // How hard the transition from screen to black is at the corners.
                float _PhosphorIntensity; // How visible the RGB phosphor dot pattern is.
            CBUFFER_END

            // Rounded rectangle SDF for corner masking.
            float RoundedRectSDF(float2 uv, float radius)
            {
                float2 d = abs(uv) - (1.0 - radius);
                return length(max(d, 0.0)) - radius;
            }

            // RGB phosphor pattern: offsets each channel slightly based on pixel column.
            half3 ApplyPhosphor(half3 col, float2 screenPos)
            {
                int pixel = (int)screenPos.x % 3;

                float3 mask = float3(1, 1, 1);
                float dim = 1.0 - _PhosphorIntensity;

                if (pixel == 0)
                    mask = float3(1.0, dim, dim);
                else if (pixel == 1)
                    mask = float3(dim, 1.0, dim);
                else
                    mask = float3(dim, dim, 1.0);

                return col * mask;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 baseUV = input.texcoord;

                // Barrel distortion.
                float2 uv = baseUV * 2.0 - 1.0;
                float2 offset = uv.yx / _Curvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;

                // Rounded corner mask.
                float2 cornerUV = uv * 2.0 - 1.0;
                float cornerDist = RoundedRectSDF(cornerUV, _CornerRadius);
                float cornerMask = 1.0 - smoothstep(0.0, 1.0 / _CornerSharpness, cornerDist);

                // Black out pixels outside the screen bounds.
                if (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0)
                    return half4(0, 0, 0, 1);

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Scanlines: darken gaps between rows based on configurable count.
                float scanline = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, _ScanlineIntensity);
                col.rgb *= scanline;

                // RGB phosphor pattern.
                if (_PhosphorIntensity > 0.001)
                {
                    float2 screenPos = uv * _ScreenParams.xy;
                    col.rgb = ApplyPhosphor(col.rgb, screenPos);
                }

                // Edge vignette.
                float2 vignetteUV = uv * 2.0 - 1.0;
                float2 vignette = _VignetteWidth / _ScreenParams.xy;
                vignette = smoothstep(0.0, vignette, 1.0 - abs(vignetteUV));
                vignette = saturate(vignette);

                col.rgb = saturate(col.rgb) * vignette.x * vignette.y * cornerMask;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
