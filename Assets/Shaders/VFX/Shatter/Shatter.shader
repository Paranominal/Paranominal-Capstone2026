// Summary: Unlit sprite shatter shader. Vertex shader displaces triangles outward from their
// Voronoi cell centers with rotation. Cell data is baked into UV2 (cell center) and vertex
// color R (cell hash). _ShatterAmount drives the animation from 0 (solid) to 1 (scattered).

Shader "Custom/Sprites/Shatter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shatter)]
        _ShatterAmount ("Shatter Amount", Range(0, 1)) = 0
        _SpreadForce ("Spread Force", Float) = 1
        _RotationSpeed ("Rotation Speed", Float) = 3
        [Toggle] _FlatShatter ("Flat Shatter (2D only)", Float) = 0

        [Header(Gravity)]
        _EnableGravity ("Enable Gravity", Float) = 0
        _GravityStrength ("Gravity Strength", Float) = 5

        [Header(Fade)]
        _FadeStart ("Fade Start", Range(0, 1)) = 0.1
        _FadeSpeed ("Fade Speed", Range(0, 10)) = 5
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
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1; // cell center (object space)
                float4 color  : COLOR;     // r = cell hash
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float  alpha : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _ShatterAmount;
            float _SpreadForce;
            float _RotationSpeed;
            float _EnableGravity;
            float _GravityStrength;
            float _FadeStart;
            float _FadeSpeed;
            float _FlatShatter;

            float3 rotateAroundAxis(float3 p, float3 axis, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                float oc = 1.0 - c;

                float3x3 rot = float3x3(
                    oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.x * axis.z + axis.y * s,
                    oc * axis.y * axis.x + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,
                    oc * axis.z * axis.x - axis.y * s,  oc * axis.z * axis.y + axis.x * s,  oc * axis.z * axis.z + c
                );

                return mul(rot, p);
            }

            v2f vert(appdata v)
            {
                v2f o;

                float seed = v.color.r;
                float3 cellCenter = float3(v.uv2.x, v.uv2.y, 0.0);
                float3 pos = v.vertex.xyz;

                if (_ShatterAmount > 0.001)
                {
                    // rotate around cell center
                    float3 localPos = pos - cellCenter;
                    float3 rotAxis;
                    if (_FlatShatter > 0.5)
                        rotAxis = float3(0, 0, 1); // spin in-plane only
                    else
                        rotAxis = normalize(float3(sin(seed * 127.1), cos(seed * 311.7), sin(seed * 419.3)));
                    float rotAngle = _ShatterAmount * _RotationSpeed * (0.5 + seed);
                    localPos = rotateAroundAxis(localPos, rotAxis, rotAngle);

                    // spread outward from cell center
                    float zSpread = (_FlatShatter > 0.5) ? 0.0 : (seed - 0.5);
                    float3 spreadDir = normalize(float3(cellCenter.x, cellCenter.y, zSpread));
                    float3 offset = spreadDir * _ShatterAmount * _SpreadForce * (0.5 + seed * 0.5);

                    // optional gravity
                    if (_EnableGravity > 0.5)
                        offset.y -= _ShatterAmount * _ShatterAmount * _GravityStrength;

                    pos = cellCenter + localPos + offset;
                }

                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                o.uv = v.uv;

                // fade out as shatter progresses
                float fadeT = saturate((_ShatterAmount - _FadeStart) / (1.0 - _FadeStart));
                o.alpha = saturate(1.0 - _FadeSpeed * fadeT);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= i.alpha;
                return col;
            }
            ENDCG
        }
    }
}
