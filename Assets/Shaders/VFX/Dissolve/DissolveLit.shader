// Summary: URP lit dissolve shader with a burning edge effect.
// Receives main light, additional lights, and shadows. 
// Use on 3D objects like magic locks.

Shader "Custom/Dissolve/Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 8
        _EdgeWidth ("Edge Width", Range(0.01, 0.3)) = 0.06

        [Header(Edge Colours)]
        [HDR] _EdgeColor1 ("Inner Edge (hot)", Color) = (3, 2.5, 0, 1)
        [HDR] _EdgeColor2 ("Outer Edge (cool)", Color) = (2, 0.3, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }

        // --- Forward Lit pass ---
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 posOS  : POSITION;
                float3 normOS : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS    : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 normWS   : TEXCOORD1;
                float3 posWS    : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _DissolveAmount;
            float _NoiseScale;
            float _EdgeWidth;
            float4 _EdgeColor1;
            float4 _EdgeColor2;

            // --- simplex noise ---

            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                       -0.577350269189626, 0.024390243902439);
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                                          + i.x + float3(0.0, i1.x, 1.0));
                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy),
                                             dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
                float3 g;
                g.x  = a0.x * x0.x  + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            // --- vertex / fragment ---

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.posWS  = TransformObjectToWorld(v.posOS.xyz);
                o.posCS  = TransformWorldToHClip(o.posWS);
                o.normWS = TransformObjectToWorldNormal(v.normOS);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // dissolve
                if (_DissolveAmount >= 0.999) discard;

                float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 noiseUV = float2(i.uv.x * aspect, i.uv.y) * _NoiseScale;
                float noise = snoise(noiseUV) * 0.5 + 0.5;

                if (_DissolveAmount > 0.001)
                    clip(noise - _DissolveAmount);

                // texture
                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

                // main light + shadows
                float4 shadowCoord = TransformWorldToShadowCoord(i.posWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 normal = normalize(i.normWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 lighting = mainLight.color * NdotL * mainLight.shadowAttenuation;

                // ambient
                float3 ambient = SampleSH(normal);

                // additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint l = 0; l < lightCount; l++)
                {
                    Light addLight = GetAdditionalLight(l, i.posWS);
                    float addNdotL = saturate(dot(normal, addLight.direction));
                    lighting += addLight.color * addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation;
                }
                #endif

                float3 result = texCol.rgb * (ambient + lighting);

                // burning edge glow
                if (_DissolveAmount > 0.001)
                {
                    float distFromEdge = noise - _DissolveAmount;
                    float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, distFromEdge);
                    float3 edgeGlow = lerp(_EdgeColor2.rgb, _EdgeColor1.rgb, edgeFactor);
                    result = lerp(result, edgeGlow, edgeFactor);
                }

                return float4(result, texCol.a);
            }
            ENDHLSL
        }

        // --- Shadow caster pass (dissolved pixels don't cast shadows) ---
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float _DissolveAmount;
            float _NoiseScale;
            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 posOS  : POSITION;
                float3 normOS : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
            };

            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                       -0.577350269189626, 0.024390243902439);
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                                          + i.x + float3(0.0, i1.x, 1.0));
                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy),
                                             dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
                float3 g;
                g.x  = a0.x * x0.x  + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            float3 _LightDirection;

            Varyings vertShadow(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.posOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(v.normOS);
                // apply shadow bias
                posWS = ApplyShadowBias(posWS, normWS, _LightDirection);
                o.posCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;
                return o;
            }

            float4 fragShadow(Varyings i) : SV_Target
            {
                if (_DissolveAmount >= 0.999) discard;
                if (_DissolveAmount > 0.001)
                {
                    float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                    float2 noiseUV = float2(i.uv.x * aspect, i.uv.y) * _NoiseScale;
                    float noise = snoise(noiseUV) * 0.5 + 0.5;
                    clip(noise - _DissolveAmount);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
