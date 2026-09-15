// Summary: Full-screen radial zoom blur + animated simplex-noise action lines for the dash.
// Zoom blur samples radially from screen center. Action lines convert UVs to polar space, sample animated simplex noise, 
// sharpen with power + remap, then mask the center clear.

Shader "Hidden/PostProcess/SpeedLines"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SpeedLines"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // zoom blur
            float _Intensity;
            float _BlurStrength;
            float _SampleCount;
            float _CenterFalloff;

            // action lines
            float4 _LinesColour;
            float _LinesTiling;
            float _LinesRadialScale;
            float _LinesPower;
            float _LinesRemap;
            float _LinesAnimation;

            // center mask
            float _MaskScale;
            float _MaskHardness;
            float _MaskPower;

            // simplex noise

            float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x)  { return mod2D289(((x * 34.0) + 1.0) * x); }

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                       -0.577350269189626, 0.024390243902439);
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);

                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);

                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod2D289(i);

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

            // fragment

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv     = input.texcoord;
                float2 center = float2(0.5, 0.5);
                float2 dir    = uv - center;
                float  dist   = length(dir);

                // Radial Zoom Blur

                float4 original    = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                float  blurMask    = smoothstep(0.0, _CenterFalloff, dist);
                float4 blurColor   = float4(0, 0, 0, 0);
                float  totalWeight = 0.0;
                int    sampleCount = max((int)_SampleCount, 1);
                float  denom       = max((float)(sampleCount - 1), 1.0);

                [loop]
                for (int i = 0; i < sampleCount; i++)
                {
                    float  t        = (float)i / denom - 0.5;
                    float2 sampleUV = uv + dir * t * _BlurStrength;
                    float  weight   = 1.0 - abs(t) * 2.0;
                    blurColor   += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV) * weight;
                    totalWeight += weight;
                }
                blurColor /= max(totalWeight, 0.001);

                float3 result = lerp(original.rgb, blurColor.rgb, _Intensity * blurMask);

                // Anime Speed Lines UwU

                // polar coordinates: radial distance + angular position
                float2 centered    = uv - center;
                float  polarRadius = length(centered) * _LinesRadialScale * 2.0;
                float  polarAngle  = atan2(centered.x, centered.y) * (1.0 / 6.28318530718) * _LinesTiling;
                float2 polarUV     = float2(polarRadius, polarAngle);

                // animate radially outward
                float2 animOffset = float2(-_LinesAnimation * _Time.y, 0.0);

                // sample noise in polar space and remap to [0, 1]
                float noise = snoise(polarUV + animOffset);
                noise = noise * 0.5 + 0.5;

                // sharpen into distinct lines
                noise = pow(noise, _LinesPower);

                // remap: cut off values below threshold, rescale remainder to [0, 1]
                float remap    = _LinesRemap;
                float lines    = saturate((noise - remap) / (1.0 - remap));

                // center mask: 0 at center, 1 toward edges
                float2 maskUV      = uv * 2.0 - 1.0;
                float  maskDist    = length(maskUV);
                float  maskEdge    = lerp(0.0, _MaskScale, _MaskHardness);
                float  maskInvLerp = (maskDist - _MaskScale) / ((maskEdge - 0.001) - _MaskScale);
                float  mask        = pow(1.0 - saturate(maskInvLerp), _MaskPower);

                // combine lines with mask, scaled by overall intensity
                float maskedLines = lines * mask * _Intensity;

                // blend toward line colour
                float3 lineRGB = _LinesColour.rgb;
                float  lineA   = _LinesColour.a;
                result = lerp(result, maskedLines * lineRGB, maskedLines * lineA);

                return float4(result, original.a);
            }
            ENDHLSL
        }
    }
}
