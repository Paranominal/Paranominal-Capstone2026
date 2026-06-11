Shader "Custom/URP/FearVignetteShader"
{
    Properties
    {
        _VignetteIntensity ("Vignette Intensity", Range(0.0, 1.0)) = 0.0
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1.0)) = 0.3
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.5
        _NoiseScale ("Noise Scale", Range(1.0, 20.0)) = 6.0
        _NoiseSpeed ("Noise Speed", Range(0.0, 2.0)) = 0.3
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
            Name "FearVignettePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _VignetteIntensity; // How far the vignette encroaches from the edges. 0 = none, 1 = full coverage.
                float _VignetteSoftness; // How gradual the vignette falloff is.
                float _NoiseIntensity; // How much the noise distorts the vignette edge.
                float _NoiseScale; // Size of the noise pattern.
                float _NoiseSpeed; // How fast the noise shifts over time.
            CBUFFER_END

            // Hash-based pseudo-random function for smooth value noise.
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Interpolated value noise using bilinear sampling of hashed grid points.
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep curve

                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Layered noise for a more organic, less uniform pattern.
            float FractalNoise(float2 p)
            {
                float value = 0.0;
                value += ValueNoise(p) * 0.5;
                value += ValueNoise(p * 2.0) * 0.25;
                value += ValueNoise(p * 4.0) * 0.125;
                return value / 0.875; // normalize to 0..1
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Radial distance from screen centre, 0 at centre, ~1 at corners.
                float2 centredUV = uv * 2.0 - 1.0;
                float radialDist = length(centredUV);

                // Sample scrolling noise based on UV position and time.
                float2 noiseUV = uv * _NoiseScale + _Time.y * _NoiseSpeed;
                float noise = FractalNoise(noiseUV);

                // Remap noise from 0..1 to -1..1 and scale by intensity.
                float noiseOffset = (noise * 2.0 - 1.0) * _NoiseIntensity;

                // Vignette radius shrinks as intensity increases. Noise offsets the edge per-pixel.
                float vignetteRadius = lerp(1.4, 0.0, _VignetteIntensity);
                float adjustedDist = radialDist + noiseOffset;

                // Smooth falloff from the vignette edge inward.
                float vignette = smoothstep(vignetteRadius, vignetteRadius + _VignetteSoftness, adjustedDist);

                col.rgb *= 1.0 - vignette;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}