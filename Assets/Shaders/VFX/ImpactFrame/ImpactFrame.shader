// Summary: Full-screen impact frame for the Special Shot. Four independently toggled layers:
// a solid flash, a two-tone thresholded scene (colour plus normals), UV jitter in angular segments,
// and radial speed lines that invert the two-tone or draw solid light over the normal scene.
// Everything is centred on _FocalPoint. Ported from the reference Shader Graph.

Shader "Hidden/PostProcess/ImpactFrame"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ImpactFrame"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            // layer toggles (0 or 1, hard cut per frame by ImpactFrameController)
            float _FlashOn;
            float _SceneOn;
            float _LinesOn;
            float _JitterOn;
            float _FlashUsesLight;

            // runtime
            float4 _FocalPoint;
            float _Seed;

            // colours
            float4 _DarkColour;
            float4 _LightColour;

            // scene threshold
            float _SceneThreshold;
            float _NormalsWeight;

            // speed lines
            float _LinesTiling;
            float _LinesNoiseScale;
            float _LinesThreshold;
            float _LinesClearMin;
            float _LinesClearMax;
            float _LinesClearPower;

            // jitter
            float _JitterScale;
            float _JitterThreshold;
            float _JitterStrength;

            // --- value noise (uses Hash_Tchou_2_1_float from Hashes.hlsl to match Shader Graph) ---

            float HashTchou(float2 p)
            {
                float result;
                Hash_Tchou_2_1_float(p, result);
                return result;
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = HashTchou(i);
                float b = HashTchou(i + float2(1.0, 0.0));
                float c = HashTchou(i + float2(0.0, 1.0));
                float d = HashTchou(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 3 octaves like Simple Noise, unnormalised 0 to 0.875 to match the graph
            float SimpleNoise(float2 uv, float scale)
            {
                float t = 0.0;
                t += ValueNoise(uv * scale)       * 0.125;
                t += ValueNoise(uv * scale / 2.0) * 0.25;
                t += ValueNoise(uv * scale / 4.0) * 0.5;
                return t;
            }

            // --- fragment ---

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv       = input.texcoord;
                float4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // --- flash, overrides every other layer ---
                if (_FlashOn > 0.5)
                {
                    float3 flash = _FlashUsesLight > 0.5 ? _LightColour.rgb : _DarkColour.rgb;
                    return float4(flash, original.a);
                }

                float aspect = _ScreenParams.x / _ScreenParams.y;

                // aspect-corrected offset from the focal point so lines stay round
                float2 offset = (uv - _FocalPoint.xy) * float2(aspect, 1.0);
                float  dist   = length(offset);
                float2 dir    = dist > 0.0001 ? offset / dist : float2(0.0, 0.0);
                float  angle  = atan2(offset.y, offset.x) + PI;

                // --- radial speed lines ---
                float lines = 0.0;
                if (_LinesOn > 0.5)
                {
                    float angleNoise = SimpleNoise(angle.xx + _Seed, _LinesTiling);
                    float radial     = dist * angleNoise;
                    float lineNoise  = SimpleNoise(radial.xx + _Seed, _LinesNoiseScale);

                    // EDIT (burst-clear): noise-driven clear zone with power curve,
                    // most spikes stay tight to the centre, a few punch out to max
                    float clearNoise = pow(angleNoise, _LinesClearPower);
                    float clearDist  = lerp(_LinesClearMin, _LinesClearMax, clearNoise);
                    lines = step(_LinesThreshold, lineNoise / 2.0) * step(clearDist, dist);
                }

                // --- UV jitter, pushes angular segments outward ---
                float2 sceneUV = uv;
                if (_JitterOn > 0.5)
                {
                    float jitterNoise = SimpleNoise(angle.xx + _Seed * 1.37, _JitterScale);
                    float jitter      = step(_JitterThreshold, jitterNoise);
                    sceneUV += dir * float2(1.0 / aspect, 1.0) * jitter * _JitterStrength;
                }

                float3 colour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sceneUV).rgb;

                // --- two-tone scene, lines invert it (XOR of the two masks) ---
                if (_SceneOn > 0.5)
                {
                    float3 normal     = SampleSceneNormals(sceneUV);
                    float  sceneValue = dot(colour, float3(0.2126, 0.7152, 0.0722)) + normal.y * _NormalsWeight;
                    float  sceneMask  = step(_SceneThreshold, sceneValue);
                    float  mask       = abs(sceneMask - lines);

                    return float4(lerp(_DarkColour.rgb, _LightColour.rgb, mask), original.a);
                }

                // --- normal scene, lines draw solid light ---
                return float4(lerp(colour, _LightColour.rgb, lines), original.a);
            }
            ENDHLSL
        }
    }
}
