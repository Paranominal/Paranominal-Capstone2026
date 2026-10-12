// Summary: Full-screen post-process shader for the dash ability. Three toggleable layers:
// radial zoom blur, UV warp distortion, and animated simplex-noise action lines.
// All layers scale with _Intensity so they fade in/out together with the dash.
// EDIT (ethereal-grade): Adds a second pass (index 1) for the Ethereal Grade layer:
// desaturation, split tone, highlight glow, and depth fade. Runs before pass 0.

Shader "Hidden/PostProcess/DashEffects"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // Pass 0: dash effects (blur, warp, lines)
        Pass
        {
            Name "DashEffects"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // master intensity (driven by PlayerDash)
            float _Intensity;

            // toggles (0 or 1)
            float _EnableBlur;
            float _EnableWarp;
            float _EnableLines;

            // zoom blur
            float _BlurStrength;
            float _SampleCount;
            float _CenterFalloff;

            // warp
            float _WarpStrength;

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

            // --- simplex noise ---

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

            // --- fragment ---

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv       = input.texcoord;
                float2 center   = float2(0.5, 0.5);
                float2 centered = uv - center;
                float  dist     = length(centered);

                // keep unwarped UVs for action lines (they're an overlay, not scene geometry)
                float2 lineUV = uv;

                // --- UV warp distortion ---
                if (_EnableWarp > 0.5)
                {
                    float2 warpOffset = centered * dist * dist * _WarpStrength * _Intensity;
                    uv += warpOffset;
                }

                // sample scene with (possibly warped) UVs
                float4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                float3 result   = original.rgb;

                // --- radial zoom blur ---
                if (_EnableBlur > 0.5)
                {
                    float2 blurDir   = uv - center;
                    float  blurDist  = length(blurDir);
                    float  blurMask  = smoothstep(0.0, _CenterFalloff, blurDist);

                    float4 blurColor   = float4(0, 0, 0, 0);
                    float  totalWeight = 0.0;
                    int    sampleCount = max((int)_SampleCount, 1);
                    float  denom       = max((float)(sampleCount - 1), 1.0);

                    [loop]
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float  t        = (float)i / denom - 0.5;
                        float2 sampleUV = uv + blurDir * t * _BlurStrength;
                        float  weight   = 1.0 - abs(t) * 2.0;
                        blurColor   += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV) * weight;
                        totalWeight += weight;
                    }
                    blurColor /= max(totalWeight, 0.001);

                    result = lerp(result, blurColor.rgb, _Intensity * blurMask);
                }

                // --- animated action lines ---
                if (_EnableLines > 0.5)
                {
                    // polar coordinates from unwarped UVs
                    float2 lineCentered = lineUV - center;
                    float  lineDist     = length(lineCentered);

                    float polarRadius = lineDist * _LinesRadialScale * 2.0;
                    float polarAngle  = atan2(lineCentered.x, lineCentered.y)
                                        * (1.0 / 6.28318530718) * _LinesTiling;
                    float2 polarUV    = float2(polarRadius, polarAngle);

                    // animate radially outward
                    float2 animOffset = float2(-_LinesAnimation * _Time.y, 0.0);

                    // sample noise in polar space, remap to [0, 1]
                    float noise = snoise(polarUV + animOffset);
                    noise = noise * 0.5 + 0.5;

                    // sharpen into distinct lines
                    noise = pow(noise, _LinesPower);

                    // cut off values below threshold
                    float remap = _LinesRemap;
                    float lines = saturate((noise - remap) / (1.0 - remap));

                    // center mask: 0 at center, 1 toward edges
                    float2 maskUV      = lineUV * 2.0 - 1.0;
                    float  maskDist    = length(maskUV);
                    float  maskEdge    = lerp(0.0, _MaskScale, _MaskHardness);
                    float  maskInvLerp = (maskDist - _MaskScale)
                                         / ((maskEdge - 0.001) - _MaskScale);
                    float  mask        = pow(1.0 - saturate(maskInvLerp), _MaskPower);

                    // combine with intensity fade
                    float maskedLines = lines * mask * _Intensity;

                    float3 lineRGB = _LinesColour.rgb;
                    float  lineA   = _LinesColour.a;
                    result = lerp(result, maskedLines * lineRGB, maskedLines * lineA);
                }

                return float4(result, original.a);
            }
            ENDHLSL
        }

        // EDIT (ethereal-grade): Pass 1: ethereal colour grade
        Pass
        {
            Name "DashEtherealGrade"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragEthereal

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // master intensity (driven by PlayerDash)
            float _Intensity;

            // grade
            float  _Desaturation;
            float4 _ShadowColour;
            float4 _HighlightColour;
            float  _SplitToneBalance;
            float  _TintStrength;

            // glow
            float _GlowThreshold;
            float _GlowIntensity;
            float _GlowSpread;

            // depth fade
            float  _EnableDepthFade;
            float4 _DepthFadeColour;
            float  _DepthFadeStart;
            float  _DepthFadeEnd;
            float  _DepthFadeStrength;

            TEXTURE2D_X(_EtherealDepthTex);

            static const float3 LumaWeights = float3(0.2126, 0.7152, 0.0722);

            // EDIT (ethereal-grade): slider value 1 maps to 0.05 actual fade
            static const float DepthFadeScale = 0.05;

            float Luma(float3 c) { return dot(c, LumaWeights); }

            // Summary: bright part of a sample above the glow threshold, 0 to 1.
            float3 BrightPass(float3 c)
            {
                float l = Luma(c);
                float b = saturate((l - _GlowThreshold) / max(1.0 - _GlowThreshold, 0.001));
                return c * b;
            }

            float4 FragEthereal(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                float3 col      = original.rgb;

                // --- glow: two rings of 8 taps around the pixel ---
                float3 glow = BrightPass(col);
                float2 aspect = float2(_ScreenParams.y / _ScreenParams.x, 1.0);

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float  angle = i * 0.785398; // 45 degrees
                    float2 dir   = float2(cos(angle), sin(angle)) * aspect * _GlowSpread;

                    glow += BrightPass(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + dir).rgb);
                    glow += BrightPass(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + dir * 2.0).rgb) * 0.5;
                }
                glow /= 13.0; // 1 + 8 + (8 * 0.5)

                // --- desaturation ---
                float lum = Luma(col);
                col = lerp(col, lum.xxx, _Desaturation);

                // --- split tone (keeps brightness, swaps hue toward shadow/highlight colour) ---
                float  toneT     = smoothstep(0.0, 1.0, saturate(lum + _SplitToneBalance * 0.5));
                float3 toneCol   = lerp(_ShadowColour.rgb, _HighlightColour.rgb, toneT);
                float3 toned     = toneCol * (lum / max(Luma(toneCol), 0.001));
                col = lerp(col, toned, _TintStrength);

                // --- depth fade ---
                if (_EnableDepthFade > 0.5)
                {
                    float rawDepth = SAMPLE_TEXTURE2D_X(_EtherealDepthTex, sampler_PointClamp, uv).r;
                    float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float fade     = saturate((eyeDepth - _DepthFadeStart)
                                              / max(_DepthFadeEnd - _DepthFadeStart, 0.001));
                    // EDIT (ethereal-grade): strength scaled down so the slider's full range is usable
                    col = lerp(col, _DepthFadeColour.rgb, fade * _DepthFadeStrength * DepthFadeScale);
                }

                // --- glow tinted toward the highlight colour, added after fade so lights bleed through ---
                float3 glowTint = lerp(glow, Luma(glow) * _HighlightColour.rgb, _TintStrength);
                col += glowTint * _GlowIntensity;

                // fade the whole grade with the dash
                float3 result = lerp(original.rgb, col, _Intensity);
                return float4(result, original.a);
            }
            ENDHLSL
        }
    }
}
