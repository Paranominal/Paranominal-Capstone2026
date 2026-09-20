// Summary: UI shader that fills from center outward based on _FillAmount (0 = empty, 1 = full).
// Drop-in replacement for UI/Default on fill bar images.

Shader "Custom/UI/MiddleOutFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _SkewAmount ("Edge Skew", Float) = 0.1

        // Unity UI stencil support (required for Mask components)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ColorMask [_ColorMask]

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
            float4 _Color;
            float _FillAmount;
            float _SkewAmount;

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

                // middle-out fill with skewed edges to match parallelogram angle
                float skewedX = i.uv.x - (i.uv.y - 0.5) * _SkewAmount;
                float distFromCenter = abs(skewedX - 0.5);
                float maxDist = 0.5 + abs(_SkewAmount) * 0.5;
                float halfFill = _FillAmount * maxDist;

                // smoothstep gives a clean anti-aliased edge
                col.a *= 1.0 - smoothstep(halfFill - 0.005, halfFill + 0.005, distFromCenter);

                return col;
            }
            ENDCG
        }
    }
}
