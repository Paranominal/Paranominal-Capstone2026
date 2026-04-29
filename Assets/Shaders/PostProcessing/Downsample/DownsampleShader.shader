// Michael Attardi - March 2026

Shader "Custom/URP/DownsampleShader"
{
    Properties
    {
        _PixelSize ("Pixel Size", Range(1, 512)) = 4 // Controls the size of each pixel block in screen pixels.
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
            Name "PixelateFullscreenPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // URP fullscreen and blit (bit block transfer) helpers.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize; // Size of each pixel block.
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord; // Use fullscreen pass UV coords to sample the current rendered screen image.

                float pixelSize = max(_PixelSize, 1.0); // Make sure that pixel block size is never less than one screen pixel.

                float2 screenPos = uv * _ScreenParams.xy; // Convert from UV space to screen pixel space.

                screenPos = floor(screenPos / pixelSize) * pixelSize; // Snap to a larger pixel grid.

                float2 pixelatedUV = (screenPos + pixelSize * 0.5) / _ScreenParams.xy; // Sample from the centre of the pixel block.

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelatedUV); // Read one colour from the whole pixel block.

                return col;
            }

            ENDHLSL
        }
    }

    FallBack Off
}