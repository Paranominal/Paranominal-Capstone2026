// Michael Attardi - March 2026

Shader "Custom/URP/Sharpen"
{
    Properties
    {
        _Sharpness ("Sharpness", Range(0, 5)) = 1.0 // Controls how strongly edges are enhanced.
        _SampleDistance ("Sample Distance (Pixels)", Range(0.25, 3.0)) = 1.0 // Controls how far away neighbouring pixels are sampled.
    }

    SubShader
    {
        Tags // Universal Render Pipeline fullscreen-compatible shader.
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        // Fullscreen post-process settings.
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "SharpenFullscreenPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // URP fullscreen and blit (bit block transfer) helpers.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Sharpness; // Strength of sharpen effect.
                float _SampleDistance; // Distance to neighbouring pixels.
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord; // Use fullscreen pass UV coords to sample the current rendered screen image.

                // Convert a pixel into UV space, then scale by sample distance.
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float2 offsetX = float2(texelSize.x * _SampleDistance, 0.0);
                float2 offsetY = float2(0.0, texelSize.y * _SampleDistance);

                // Sample the current pixel and its four direct neighbours (up, down, left, right).
                half4 center = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 left   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offsetX);
                half4 right  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offsetX);
                half4 up     = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offsetY);
                half4 down   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offsetY);

                float s = max(_Sharpness, 0.0); // Clamp sharpening strength to a non-negative value.
                
                float3 sharpened = center.rgb * (1.0 + 4.0 * s) - (left.rgb + right.rgb + up.rgb + down.rgb) * s; // Boost the centre and subtract neighbouring colour (center * (1 + 4s) - neighbours * s).

                sharpened = saturate(sharpened); // Clamp back to a valid colour range (0-1).

                return half4(sharpened, center.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}