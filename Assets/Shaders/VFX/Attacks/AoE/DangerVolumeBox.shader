Shader "Custom/URP/DangerVolumeBox"
{
    Properties
    {
        [Header(Main Colours)]
        _FillColor ("Fill Color", Color) = (1, 0, 0, 0.12)
        _FaceColor ("Face Highlight Color", Color) = (1, 0.25, 0.25, 1)
        _EdgeColor ("Edge Highlight Color", Color) = (1, 0.8, 0.8, 1)

        [Header(Fill Settings)]
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0.12

        [Header(Face Shell Settings)]
        _FaceThickness ("Face Shell Thickness (World Units)", Range(0.001, 0.5)) = 0.08
        _FaceIntensity ("Face Intensity", Range(0, 8)) = 1.5
        _FaceOpacity ("Face Opacity", Range(0, 1)) = 0.6

        [Header(Edge Settings)]
        _EdgeThickness ("Edge Thickness (World Units)", Range(0.001, 0.5)) = 0.05
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 3.0
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 1.0

        [Header(Box Bounds)]
        _HalfExtents ("Half Extents", Vector) = (0.5, 0.5, 0.5, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "DangerVolumeBoxPass"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _FaceColor;
                float4 _EdgeColor;

                float _FillOpacity;

                float _FaceThickness;
                float _FaceIntensity;
                float _FaceOpacity;

                float _EdgeThickness;
                float _EdgeIntensity;
                float _EdgeOpacity;

                float4 _HalfExtents;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            // Extracts the world-space scale (per-axis) from the object-to-world matrix.
            // Each column's length is the magnitude of that axis's basis vector in world
            // space, which equals the transform's scale on that axis (assuming no shear).
            float3 GetObjectScale()
            {
                return float3(
                    length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                    length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                    length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 halfExtents = max(_HalfExtents.xyz, float3(0.0001, 0.0001, 0.0001));

                // Per-axis world-space scale of this transform. Used to convert
                // the object-space distance-to-face values into world units, so
                // that _FaceThickness and _EdgeThickness behave consistently no
                // matter how the box is non-uniformly scaled.
                float3 objectScale = GetObjectScale();

                // Distance from this fragment to each outer face of the box,
                // converted from object space to world units by the transform's scale.
                float3 distToFacesWS = (halfExtents - abs(IN.positionOS)) * objectScale;

                // Nearest face distance in world units.
                float nearestFaceDist = min(distToFacesWS.x, min(distToFacesWS.y, distToFacesWS.z));

                // Build a face-shell mask near the outside of the prism.
                float faceMask = 1.0 - smoothstep(0.0, _FaceThickness, nearestFaceDist);

                // Edge detection in world units, so each axis's edge band has
                // identical visible thickness regardless of scale.
                float edgeX = 1.0 - smoothstep(0.0, _EdgeThickness, distToFacesWS.x);
                float edgeY = 1.0 - smoothstep(0.0, _EdgeThickness, distToFacesWS.y);
                float edgeZ = 1.0 - smoothstep(0.0, _EdgeThickness, distToFacesWS.z);

                // Pairwise combinations highlight actual box edges.
                float edgeMask = max(edgeX * edgeY, max(edgeX * edgeZ, edgeY * edgeZ));

                // Small Fresnel contribution helps readability from oblique angles.
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float fresnelBoost = pow(fresnel, 2.0);

                float3 fillRGB = _FillColor.rgb;
                float3 faceRGB = _FaceColor.rgb * faceMask * _FaceIntensity * (1.0 + fresnelBoost * 0.35);
                float3 edgeRGB = _EdgeColor.rgb * edgeMask * _EdgeIntensity * (1.0 + fresnelBoost * 0.5);

                float fillAlpha = saturate(_FillOpacity * _FillColor.a);
                float faceAlpha = saturate(faceMask * _FaceOpacity * _FaceColor.a);
                float edgeAlpha = saturate(edgeMask * _EdgeOpacity * _EdgeColor.a);

                float3 finalRGB = fillRGB + faceRGB + edgeRGB;
                float finalAlpha = saturate(fillAlpha + faceAlpha + edgeAlpha);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
