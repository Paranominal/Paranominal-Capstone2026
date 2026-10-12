Shader "UI/NoiseVignetteTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0,1)) = 0
        _Softness ("Softness", Range(0.01,1)) = 0.15
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.5
        _NoiseScale ("Noise Scale", Range(1,20)) = 6
        _NoiseSpeed ("Noise Speed", Float) = 0.3
        _CycleDuration ("Cycle Duration", Float) = 10
        _Seed ("Seed", Float) = 0
        _ManualTime ("Manual Time", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float _Progress, _Softness, _NoiseIntensity, _NoiseScale;
            float _NoiseSpeed, _CycleDuration, _Seed, _ManualTime;

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FractalNoise(float2 p)
            {
                float v = ValueNoise(p) * 0.5;
                v += ValueNoise(p * 2.0) * 0.25;
                v += ValueNoise(p * 4.0) * 0.125;
                return v / 0.875;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Radial distance from screen centre: 0 at centre, ~1.414 at corners
                float2 centredUV = uv * 2.0 - 1.0;
                float radialDist = length(centredUV);
                float2 inwardDir = normalize(centredUV + 0.0001);

                // Dual-layer crossfade so the inward creep never visibly stretches
                float cyclePeriod = max(_CycleDuration * _NoiseSpeed, 0.0001);
                float t1 = fmod(_ManualTime * _NoiseSpeed, cyclePeriod);
                float t2 = fmod(_ManualTime * _NoiseSpeed + cyclePeriod * 0.5, cyclePeriod);

                float2 baseUV = uv * _NoiseScale + _Seed;
                float noise1 = FractalNoise(baseUV + inwardDir * t1);
                float noise2 = FractalNoise(baseUV + inwardDir * t2);

                float blend = abs(t1 / cyclePeriod * 2.0 - 1.0);
                float noise = lerp(noise1, noise2, blend);

                float noiseOffset = (noise * 2.0 - 1.0) * _NoiseIntensity;
                float adjustedDist = radialDist + noiseOffset;

                // Edge radius travels from "beyond every pixel, even with noise" (progress 0)
                // to "behind every pixel, even with noise" (progress 1)
                float margin = 0.05;
                float startR = 1.4143 + _NoiseIntensity + margin;
                float endR = -_NoiseIntensity - _Softness - margin;
                float radius = lerp(startR, endR, _Progress);

                float alpha = smoothstep(radius, radius + _Softness, adjustedDist);

                fixed4 col = i.color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}