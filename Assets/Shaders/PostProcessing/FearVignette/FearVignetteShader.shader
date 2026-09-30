Shader "Custom/URP/FearVignetteShader"
{
    Properties
    {
        _VignetteIntensity ("Vignette Intensity", Range(0.0, 1.0)) = 0.0
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1.0)) = 0.3
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.5
        _NoiseScale ("Noise Scale", Range(1.0, 20.0)) = 6.0
        _NoiseSpeed ("Noise Speed", Float) = 0.3
        _CycleDuration ("Cycle Duration (Seconds)", Float) = 10.0
        _BlendMode ("Blend Mode", Float) = 0.0
        _Enabled ("Enabled", Float) = 1.0
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
                float4 _VignetteColor; // Colour the vignette fades toward.
                float _NoiseIntensity; // How much the noise distorts the vignette edge.
                float _NoiseScale; // Size of the noise pattern.
                float _NoiseSpeed; // How fast the noise creeps inward.
                float _CycleDuration; // How long in seconds before each noise layer resets.
                float _BlendMode; // 0 = Multiply, 1 = Screen, 2 = Overlay, 3 = Hard Light.
                float _Enabled; // Toggle to bypass the effect for Scene View camera.
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

            // Blend mode functions.
            half3 BlendMultiply(half3 base, half3 blend)
            {
                return base * blend;
            }

            half3 BlendScreen(half3 base, half3 blend)
            {
                return 1.0 - (1.0 - base) * (1.0 - blend);
            }

            half3 BlendOverlay(half3 base, half3 blend)
            {
                // Per-channel: darkens where base is dark, lightens where base is light.
                return lerp(
                    2.0 * base * blend,
                    1.0 - 2.0 * (1.0 - base) * (1.0 - blend),
                    step(0.5, base)
                );
            }

            half3 BlendHardLight(half3 base, half3 blend)
            {
                // Same as overlay but driven by blend colour instead of base.
                return lerp(
                    2.0 * base * blend,
                    1.0 - 2.0 * (1.0 - base) * (1.0 - blend),
                    step(0.5, blend)
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Bypass the effect when disabled (Scene View camera).
                if (_Enabled < 0.5)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Radial distance from screen centre, 0 at centre, ~1 at corners.
                float2 centredUV = uv * 2.0 - 1.0;
                float radialDist = length(centredUV);

                // Direction from this pixel toward the screen centre, used to scroll noise inward.
                float2 inwardDir = normalize(centredUV + 0.0001); // small bias to avoid zero at exact centre

                // Dual-layer crossfade: two noise layers offset by half a cycle, blended so each
                // fades out before it stretches noticeably. Keeps the inward creep without distortion.
                float cyclePeriod = _CycleDuration * _NoiseSpeed;
                float t1 = fmod(_Time.y * _NoiseSpeed, cyclePeriod);
                float t2 = fmod(_Time.y * _NoiseSpeed + cyclePeriod * 0.5, cyclePeriod);

                float2 noiseUV1 = uv * _NoiseScale + inwardDir * t1;
                float2 noiseUV2 = uv * _NoiseScale + inwardDir * t2;

                float noise1 = FractalNoise(noiseUV1);
                float noise2 = FractalNoise(noiseUV2);

                // Crossfade: each layer peaks at mid-cycle, fades at reset point.
                float blend = abs(t1 / cyclePeriod * 2.0 - 1.0);
                float noise = lerp(noise1, noise2, blend);

                // Remap noise from 0..1 to -1..1 and scale by intensity.
                float noiseOffset = (noise * 2.0 - 1.0) * _NoiseIntensity;

                // Vignette radius shrinks as intensity increases. Noise offsets the edge per-pixel.
                float vignetteRadius = lerp(1.4, 0.0, _VignetteIntensity);
                float adjustedDist = radialDist + noiseOffset;

                // Smooth falloff from the vignette edge inward.
                float vignette = smoothstep(vignetteRadius, vignetteRadius + _VignetteSoftness, adjustedDist);

                // Apply selected blend mode between scene colour and vignette colour.
                half3 blended;
                int mode = (int)_BlendMode;

                if (mode == 1)
                    blended = BlendScreen(col.rgb, _VignetteColor.rgb);
                else if (mode == 2)
                    blended = BlendOverlay(col.rgb, _VignetteColor.rgb);
                else if (mode == 3)
                    blended = BlendHardLight(col.rgb, _VignetteColor.rgb);
                else
                    blended = BlendMultiply(col.rgb, _VignetteColor.rgb);

                // Lerp between original and blended based on vignette mask.
                col.rgb = lerp(col.rgb, blended, vignette);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
