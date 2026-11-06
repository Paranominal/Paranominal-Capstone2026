Shader "Custom/URP/DownsampleShader"
{
    Properties
    {
        _PixelSize ("Pixel Size", Range(1, 512)) = 4 // Size of each pixel block in screen pixels. EDIT (Downsample-Dither): Already resolution-scaled and rounded by DownsampleRendererFeature.
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

                // EDIT (Downsample-Dither): Resolution scaling and rounding moved to the renderer feature so this matches the dither cell size exactly.
                int pixelSize = max(1, (int)_PixelSize);

                int2 pixelPos = int2(input.positionCS.xy);
                int2 blockOrigin = (pixelPos / pixelSize) * pixelSize; // Snap to the block grid.
                int2 samplePos = blockOrigin + pixelSize / 2; // Pick one whole pixel inside the block.

                half4 col = LOAD_TEXTURE2D_X(_BlitTexture, samplePos);

                return col;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
