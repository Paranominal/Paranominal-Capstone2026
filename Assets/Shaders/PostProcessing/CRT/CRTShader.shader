Shader "Custom/URP/CRTShader"
{
    Properties
    {
        _Curvature ("Curvature", Range(1.0, 10.0)) = 1.0 // Controls how strongly the image curves toward the screen edges.
        _VignetteWidth ("Vignette Width", Range(1.0, 100.0)) = 30.0 // Controls how wide the vignette fade is at the edges.
        _OverlayTex ("Overlay Texture", 2D) = "white" {} // Optional texture that can be shown instead of the camera image.
        _UseOverlayTex ("Use Overlay Texture", Float) = 0
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
                float _Curvature; // Curvature amount for the barrel distortion seen on old curved glass CRT screens.
                float _VignetteWidth; // Width of the darkened edge falloff.
                float _UseOverlayTex; // Toggle between using the screen image and the overlay texture.
            CBUFFER_END

            // Optional overlay texture sampled when requested.
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 baseUV = input.texcoord; // Use fullscreen pass UV coords to sample the current rendered screen image.

                // Convert UV coords into a centred -1 to 1 range for distortion.
                float2 uv = baseUV * 2.0 - 1.0;

                // Apply a simple CRT-style barrel distortion.
                float2 offset = uv.yx / _Curvature;
                uv = uv + uv * offset * offset;

                // Convert back into normal 0 to 1 UV space.
                uv = uv * 0.5 + 0.5;

                half4 col;

                // Either sample the overlay texture or the rendered screen image.
                if (_UseOverlayTex > 0.5)
                {
                    col = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, uv);
                }
                else
                {
                    col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }

                // Black out the pixels that have been pushed outside the screen bounds.
                if (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0)
                {
                    col = half4(0, 0, 0, 1);
                }

                // Build a vignette mask based on distance from the screen edges.
                float2 vignetteUV = uv * 2.0 - 1.0;
                float2 vignette = _VignetteWidth / _ScreenParams.xy;
                vignette = smoothstep(0.0, vignette, 1.0 - abs(vignetteUV));
                vignette = saturate(vignette);

                // Add slight alternating colour variation to mimic CRT scanline tinting.
                col.g *= (sin(baseUV.y * _ScreenParams.y * 2.0) + 1.0) * 0.15 + 1.0;
                col.rb *= (cos(baseUV.y * _ScreenParams.y * 2.0) + 1.0) * 0.135 + 1.0;

                col.rgb = saturate(col.rgb) * vignette.x * vignette.y; // Clamp colour and apply the vignette darkening.

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}