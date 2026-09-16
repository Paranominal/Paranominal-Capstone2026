// Summary: Unlit sprite dissolve shader with a burning edge effect.
// Simplex noise determines the dissolve pattern. _DissolveAmount controls the threshold (0 = solid, 1 = fully dissolved). 
// Pixels near the dissolve edge receive a two-colour burning glow.

Shader "Custom/Sprites/Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * i.color;

                // nothing to dissolve when fully solid
                if (_DissolveAmount <= 0.001) return col;

                // sample noise with aspect correction so it doesn't stretch on wide sprites
                float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 noiseUV = float2(i.uv.x * aspect, i.uv.y) * _NoiseScale;
                float noise = snoise(noiseUV) * 0.5 + 0.5;

                // clip dissolved pixels
                float threshold = _DissolveAmount;
                clip(noise - threshold);

                // fully dissolved safety (noise peaks could survive at 1.0)
                if (_DissolveAmount >= 0.999) discard;

                // burning edge: blend hot -> cool based on proximity to the clip boundary
                float distFromEdge = noise - threshold;
                float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, distFromEdge);
                float3 edgeGlow = lerp(_EdgeColor2.rgb, _EdgeColor1.rgb, edgeFactor);

                // add glow onto the sprite (HDR colours create bloom-ready brightness)
                col.rgb = lerp(col.rgb, edgeGlow, edgeFactor);

                // preserve original sprite alpha for non-rectangular sprites
                return col;
            }
            ENDCG
        }
    }
}
