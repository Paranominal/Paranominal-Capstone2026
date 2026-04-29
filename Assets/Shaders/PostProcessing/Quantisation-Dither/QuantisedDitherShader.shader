Shader "Custom/URP/QuantisedDitherShader"
{
    Properties
    {
        _RedSteps ("Red Steps", Range(2, 32)) = 8
        _GreenSteps ("Green Steps", Range(2, 32)) = 8
        _BlueSteps ("Blue Steps", Range(2, 32)) = 8

        _EffectStrength ("Effect Strength", Range(0, 1)) = 1
        _DitherStrength ("Dither Strength", Range(0, 1)) = 1

        _BayerSize ("Bayer Matrix Size", Float) = 16
        _UsePS1Matrix ("Use PS1 Matrix", Float) = 0

        _UsePerceivedBrightness ("Use Perceived Brightness", Float) = 0
        _PerceptualGamma ("Perceptual Gamma", Range(0.2, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "QuantisedDitherPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _RedSteps;
                float _GreenSteps;
                float _BlueSteps;

                float _EffectStrength;
                float _DitherStrength;

                float _BayerSize;
                float _UsePS1Matrix;

                float _UsePerceivedBrightness;
                float _PerceptualGamma;
            CBUFFER_END

            float Bayer2x2(int x, int y)
            {
                static const float bayer[4] =
                {
                    0.0 / 4.0, 2.0 / 4.0,
                    3.0 / 4.0, 1.0 / 4.0
                };

                int ix = x & 1;
                int iy = y & 1;
                return bayer[iy * 2 + ix];
            }

            float Bayer4x4(int x, int y)
            {
                static const float bayer[16] =
                {
                     0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                    12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                     3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                    15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
                };

                int ix = x & 3;
                int iy = y & 3;
                return bayer[iy * 4 + ix];
            }

            float Bayer8x8(int x, int y)
            {
                static const float bayer[64] =
                {
                     0.0 / 64.0, 32.0 / 64.0,  8.0 / 64.0, 40.0 / 64.0,  2.0 / 64.0, 34.0 / 64.0, 10.0 / 64.0, 42.0 / 64.0,
                    48.0 / 64.0, 16.0 / 64.0, 56.0 / 64.0, 24.0 / 64.0, 50.0 / 64.0, 18.0 / 64.0, 58.0 / 64.0, 26.0 / 64.0,
                    12.0 / 64.0, 44.0 / 64.0,  4.0 / 64.0, 36.0 / 64.0, 14.0 / 64.0, 46.0 / 64.0,  6.0 / 64.0, 38.0 / 64.0,
                    60.0 / 64.0, 28.0 / 64.0, 52.0 / 64.0, 20.0 / 64.0, 62.0 / 64.0, 30.0 / 64.0, 54.0 / 64.0, 22.0 / 64.0,
                     3.0 / 64.0, 35.0 / 64.0, 11.0 / 64.0, 43.0 / 64.0,  1.0 / 64.0, 33.0 / 64.0,  9.0 / 64.0, 41.0 / 64.0,
                    51.0 / 64.0, 19.0 / 64.0, 59.0 / 64.0, 27.0 / 64.0, 49.0 / 64.0, 17.0 / 64.0, 57.0 / 64.0, 25.0 / 64.0,
                    15.0 / 64.0, 47.0 / 64.0,  7.0 / 64.0, 39.0 / 64.0, 13.0 / 64.0, 45.0 / 64.0,  5.0 / 64.0, 37.0 / 64.0,
                    63.0 / 64.0, 31.0 / 64.0, 55.0 / 64.0, 23.0 / 64.0, 61.0 / 64.0, 29.0 / 64.0, 53.0 / 64.0, 21.0 / 64.0
                };

                int ix = x & 7;
                int iy = y & 7;
                return bayer[iy * 8 + ix];
            }

            float Bayer16x16(int x, int y)
            {
                static const float bayer[256] =
                {
                      0.0 / 256.0, 128.0 / 256.0,  32.0 / 256.0, 160.0 / 256.0,   8.0 / 256.0, 136.0 / 256.0,  40.0 / 256.0, 168.0 / 256.0,   2.0 / 256.0, 130.0 / 256.0,  34.0 / 256.0, 162.0 / 256.0,  10.0 / 256.0, 138.0 / 256.0,  42.0 / 256.0, 170.0 / 256.0,
                    192.0 / 256.0,  64.0 / 256.0, 224.0 / 256.0,  96.0 / 256.0, 200.0 / 256.0,  72.0 / 256.0, 232.0 / 256.0, 104.0 / 256.0, 194.0 / 256.0,  66.0 / 256.0, 226.0 / 256.0,  98.0 / 256.0, 202.0 / 256.0,  74.0 / 256.0, 234.0 / 256.0, 106.0 / 256.0,
                     48.0 / 256.0, 176.0 / 256.0,  16.0 / 256.0, 144.0 / 256.0,  56.0 / 256.0, 184.0 / 256.0,  24.0 / 256.0, 152.0 / 256.0,  50.0 / 256.0, 178.0 / 256.0,  18.0 / 256.0, 146.0 / 256.0,  58.0 / 256.0, 186.0 / 256.0,  26.0 / 256.0, 154.0 / 256.0,
                    240.0 / 256.0, 112.0 / 256.0, 208.0 / 256.0,  80.0 / 256.0, 248.0 / 256.0, 120.0 / 256.0, 216.0 / 256.0,  88.0 / 256.0, 242.0 / 256.0, 114.0 / 256.0, 210.0 / 256.0,  82.0 / 256.0, 250.0 / 256.0, 122.0 / 256.0, 218.0 / 256.0,  90.0 / 256.0,

                     12.0 / 256.0, 140.0 / 256.0,  44.0 / 256.0, 172.0 / 256.0,   4.0 / 256.0, 132.0 / 256.0,  36.0 / 256.0, 164.0 / 256.0,  14.0 / 256.0, 142.0 / 256.0,  46.0 / 256.0, 174.0 / 256.0,   6.0 / 256.0, 134.0 / 256.0,  38.0 / 256.0, 166.0 / 256.0,
                    204.0 / 256.0,  76.0 / 256.0, 236.0 / 256.0, 108.0 / 256.0, 196.0 / 256.0,  68.0 / 256.0, 228.0 / 256.0, 100.0 / 256.0, 206.0 / 256.0,  78.0 / 256.0, 238.0 / 256.0, 110.0 / 256.0, 198.0 / 256.0,  70.0 / 256.0, 230.0 / 256.0, 102.0 / 256.0,
                     60.0 / 256.0, 188.0 / 256.0,  28.0 / 256.0, 156.0 / 256.0,  52.0 / 256.0, 180.0 / 256.0,  20.0 / 256.0, 148.0 / 256.0,  62.0 / 256.0, 190.0 / 256.0,  30.0 / 256.0, 158.0 / 256.0,  54.0 / 256.0, 182.0 / 256.0,  22.0 / 256.0, 150.0 / 256.0,
                    252.0 / 256.0, 124.0 / 256.0, 220.0 / 256.0,  92.0 / 256.0, 244.0 / 256.0, 116.0 / 256.0, 212.0 / 256.0,  84.0 / 256.0, 254.0 / 256.0, 126.0 / 256.0, 222.0 / 256.0,  94.0 / 256.0, 246.0 / 256.0, 118.0 / 256.0, 214.0 / 256.0,  86.0 / 256.0,

                      3.0 / 256.0, 131.0 / 256.0,  35.0 / 256.0, 163.0 / 256.0,  11.0 / 256.0, 139.0 / 256.0,  43.0 / 256.0, 171.0 / 256.0,   1.0 / 256.0, 129.0 / 256.0,  33.0 / 256.0, 161.0 / 256.0,   9.0 / 256.0, 137.0 / 256.0,  41.0 / 256.0, 169.0 / 256.0,
                    195.0 / 256.0,  67.0 / 256.0, 227.0 / 256.0,  99.0 / 256.0, 203.0 / 256.0,  75.0 / 256.0, 235.0 / 256.0, 107.0 / 256.0, 193.0 / 256.0,  65.0 / 256.0, 225.0 / 256.0,  97.0 / 256.0, 201.0 / 256.0,  73.0 / 256.0, 233.0 / 256.0, 105.0 / 256.0,
                     51.0 / 256.0, 179.0 / 256.0,  19.0 / 256.0, 147.0 / 256.0,  59.0 / 256.0, 187.0 / 256.0,  27.0 / 256.0, 155.0 / 256.0,  49.0 / 256.0, 177.0 / 256.0,  17.0 / 256.0, 145.0 / 256.0,  57.0 / 256.0, 185.0 / 256.0,  25.0 / 256.0, 153.0 / 256.0,
                    243.0 / 256.0, 115.0 / 256.0, 211.0 / 256.0,  83.0 / 256.0, 251.0 / 256.0, 123.0 / 256.0, 219.0 / 256.0,  91.0 / 256.0, 241.0 / 256.0, 113.0 / 256.0, 209.0 / 256.0,  81.0 / 256.0, 249.0 / 256.0, 121.0 / 256.0, 217.0 / 256.0,  89.0 / 256.0,

                     15.0 / 256.0, 143.0 / 256.0,  47.0 / 256.0, 175.0 / 256.0,   7.0 / 256.0, 135.0 / 256.0,  39.0 / 256.0, 167.0 / 256.0,  13.0 / 256.0, 141.0 / 256.0,  45.0 / 256.0, 173.0 / 256.0,   5.0 / 256.0, 133.0 / 256.0,  37.0 / 256.0, 165.0 / 256.0,
                    207.0 / 256.0,  79.0 / 256.0, 239.0 / 256.0, 111.0 / 256.0, 199.0 / 256.0,  71.0 / 256.0, 231.0 / 256.0, 103.0 / 256.0, 205.0 / 256.0,  77.0 / 256.0, 237.0 / 256.0, 109.0 / 256.0, 197.0 / 256.0,  69.0 / 256.0, 229.0 / 256.0, 101.0 / 256.0,
                     63.0 / 256.0, 191.0 / 256.0,  31.0 / 256.0, 159.0 / 256.0,  55.0 / 256.0, 183.0 / 256.0,  23.0 / 256.0, 151.0 / 256.0,  61.0 / 256.0, 189.0 / 256.0,  29.0 / 256.0, 157.0 / 256.0,  53.0 / 256.0, 181.0 / 256.0,  21.0 / 256.0, 149.0 / 256.0,
                    255.0 / 256.0, 127.0 / 256.0, 223.0 / 256.0,  95.0 / 256.0, 247.0 / 256.0, 119.0 / 256.0, 215.0 / 256.0,  87.0 / 256.0, 253.0 / 256.0, 125.0 / 256.0, 221.0 / 256.0,  93.0 / 256.0, 245.0 / 256.0, 117.0 / 256.0, 213.0 / 256.0,  85.0 / 256.0
                };

                int ix = x & 15;
                int iy = y & 15;
                return bayer[iy * 16 + ix];
            }

            float PS1Matrix4x4(int x, int y)
            {
                static const float ps1[16] =
                {
                    -4.0,  0.0, -3.0,  1.0,
                     2.0, -2.0,  3.0, -1.0,
                    -3.0,  1.0, -4.0,  0.0,
                     3.0, -1.0,  2.0, -2.0
                };

                int ix = x & 3;
                int iy = y & 3;

                // Normalise to a similar working range as a signed threshold.
                return ps1[iy * 4 + ix] / 8.0;
            }

            float GetDitherThreshold(int x, int y)
            {
                if (_UsePS1Matrix > 0.5)
                {
                    return PS1Matrix4x4(x, y);
                }

                float threshold = 0.0;

                if (_BayerSize < 3.0)
                    threshold = Bayer2x2(x, y);
                else if (_BayerSize < 6.0)
                    threshold = Bayer4x4(x, y);
                else if (_BayerSize < 12.0)
                    threshold = Bayer8x8(x, y);
                else
                    threshold = Bayer16x16(x, y);

                return threshold - 0.5;
            }

            float GetLinearLuminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float GetBrightnessForMode(float3 color)
            {
                float luminance = GetLinearLuminance(color);

                if (_UsePerceivedBrightness > 0.5)
                {
                    return pow(saturate(luminance), _PerceptualGamma);
                }

                return luminance;
            }

            float QuantiseChannel(float value, float steps, float thresholdOffset)
            {
                steps = max(steps, 2.0);

                // Use the dither threshold as a bias during quantisation.
                float biasedValue = value * steps + thresholdOffset * _DitherStrength;

                return floor(biasedValue) / steps;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 pixelPos = uv * _ScreenParams.xy;
                float thresholdOffset = GetDitherThreshold((int)pixelPos.x, (int)pixelPos.y);

                float3 sourceColor = col.rgb;

                if (_UsePerceivedBrightness > 0.5)
                {
                    // Apply the perceptual curve before quantisation so the dither pattern
                    // influences perceived brightness rather than raw linear luminance.
                    float3 perceivedColor;
                    perceivedColor.r = pow(saturate(sourceColor.r), _PerceptualGamma);
                    perceivedColor.g = pow(saturate(sourceColor.g), _PerceptualGamma);
                    perceivedColor.b = pow(saturate(sourceColor.b), _PerceptualGamma);

                    float3 quantisedPerceived;
                    quantisedPerceived.r = QuantiseChannel(perceivedColor.r, _RedSteps, thresholdOffset);
                    quantisedPerceived.g = QuantiseChannel(perceivedColor.g, _GreenSteps, thresholdOffset);
                    quantisedPerceived.b = QuantiseChannel(perceivedColor.b, _BlueSteps, thresholdOffset);

                    float inverseGamma = 1.0 / max(_PerceptualGamma, 0.0001);

                    sourceColor.r = pow(saturate(quantisedPerceived.r), inverseGamma);
                    sourceColor.g = pow(saturate(quantisedPerceived.g), inverseGamma);
                    sourceColor.b = pow(saturate(quantisedPerceived.b), inverseGamma);
                }
                else
                {
                    float3 quantised;
                    quantised.r = QuantiseChannel(sourceColor.r, _RedSteps, thresholdOffset);
                    quantised.g = QuantiseChannel(sourceColor.g, _GreenSteps, thresholdOffset);
                    quantised.b = QuantiseChannel(sourceColor.b, _BlueSteps, thresholdOffset);

                    sourceColor = quantised;
                }

                float3 result = lerp(col.rgb, saturate(sourceColor), saturate(_EffectStrength));
                return half4(result, col.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}