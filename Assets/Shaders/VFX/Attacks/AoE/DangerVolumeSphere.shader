Shader "Custom/URP/DangerVolumeSphere"
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
        _FacePower ("Face Falloff Power", Range(0.1, 8)) = 1.8
        _FaceIntensity ("Face Intensity", Range(0, 8)) = 1.5
        _FaceOpacity ("Face Opacity", Range(0, 1)) = 0.6

        [Header(Edge Settings)]
        _EdgePower ("Edge Falloff Power", Range(1, 16)) = 7.0
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 3.0
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 1.0

        [Header(Ground Intersection Settings)]
        [Toggle] _UseGroundIntersection ("Enable Ground Intersection", Float) = 1
        _GroundColor ("Ground Intersection Color", Color) = (1, 0.9, 0.6, 1)
        _GroundThickness ("Ground Ring Thickness (World Units)", Range(0.001, 2.0)) = 0.15
        _GroundIntensity ("Ground Ring Intensity", Range(0, 8)) = 4.0
        _GroundOpacity ("Ground Ring Opacity", Range(0, 1)) = 1.0
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
            Name "DangerVolumeSpherePass"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _FaceColor;
                float4 _EdgeColor;

                float _FillOpacity;

                float _FacePower;
                float _FaceIntensity;
                float _FaceOpacity;

                float _EdgePower;
                float _EdgeIntensity;
                float _EdgeOpacity;

                float _UseGroundIntersection;
                float4 _GroundColor;
                float _GroundThickness;
                float _GroundIntensity;
                float _GroundOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.screenPos = ComputeScreenPos(positionInputs.positionCS);

                return OUT;
            }

            // Reconstruct the world-space position of the scene at a given screen UV
            // by sampling the depth texture and unprojecting through the inverse VP matrix.
            float3 ReconstructSceneWorldPos(float2 screenUV)
            {
                float rawSceneDepth = SampleSceneDepth(screenUV);

                // Build a clip-space position from the screen UV and the sampled depth.
                // Y is flipped on some platforms, but ComputeWorldSpacePosition handles
                // that internally via UNITY_MATRIX_I_VP.
                #if UNITY_REVERSED_Z
                    float clipZ = rawSceneDepth;
                #else
                    float clipZ = rawSceneDepth * 2.0 - 1.0;
                #endif

                return ComputeWorldSpacePosition(screenUV, rawSceneDepth, UNITY_MATRIX_I_VP);
            }

            // VFACE is +1 for front-faces, -1 for back-faces.
            half4 frag(Varyings IN, float facing : VFACE) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);

                if (facing < 0.0) normalWS = -normalWS;

                // Base Fresnel term: 1 at silhouette, 0 facing the camera.
                float ndotv = saturate(dot(normalWS, viewDirWS));
                float rim = 1.0 - ndotv;

                float faceMask = pow(rim, _FacePower);
                float edgeMask = pow(rim, _EdgePower);

                // Suppress silhouette edge on back-faces to avoid a doubled inner ring.
                if (facing < 0.0) edgeMask = 0.0;

                // ---- Ground intersection ring ----
                // The previous version compared screen-space depths, which lit
                // up the ring wherever the sphere's drawn fragment happened to
                // share a camera-space depth with the scene behind it - that's
                // an approximation of intersection that breaks down at angles.
                //
                // This version reconstructs the actual world-space position of
                // the scene under each pixel, then measures the true 3D distance
                // from this sphere fragment to that scene point. The ring lights
                // up where the sphere surface is genuinely close to a piece of
                // world geometry, regardless of camera orientation.
                float groundMask = 0.0;
                if (_UseGroundIntersection > 0.5)
                {
                    float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                    float3 sceneWorldPos = ReconstructSceneWorldPos(screenUV);

                    // True 3D distance between this sphere fragment and the
                    // scene surface drawn at the same screen pixel.
                    float worldDist = distance(IN.positionWS, sceneWorldPos);

                    groundMask = 1.0 - smoothstep(0.0, _GroundThickness, worldDist);

                    // Suppress the ring on front-facing fragments. The actual
                    // sphere/ground intersection is on the *back* surface of the
                    // sphere from the camera's view (the underside), so drawing
                    // the ring on front-faces too would create a phantom band
                    // floating in front of the real intersection.
                    // if (facing > 0.0) groundMask = 0.0;
                }

                float3 fillRGB = _FillColor.rgb;
                float3 faceRGB = _FaceColor.rgb * faceMask * _FaceIntensity;
                float3 edgeRGB = _EdgeColor.rgb * edgeMask * _EdgeIntensity;
                float3 groundRGB = _GroundColor.rgb * groundMask * _GroundIntensity;

                float fillAlpha = saturate(_FillOpacity * _FillColor.a);
                float faceAlpha = saturate(faceMask * _FaceOpacity * _FaceColor.a);
                float edgeAlpha = saturate(edgeMask * _EdgeOpacity * _EdgeColor.a);
                float groundAlpha = saturate(groundMask * _GroundOpacity * _GroundColor.a);

                float3 finalRGB = fillRGB + faceRGB + edgeRGB + groundRGB;
                float finalAlpha = saturate(fillAlpha + faceAlpha + edgeAlpha + groundAlpha);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
