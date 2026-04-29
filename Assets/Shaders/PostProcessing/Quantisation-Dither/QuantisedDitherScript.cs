using UnityEngine;

[ExecuteAlways]
public class QuantisedDitherScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material quantisedDitherMaterial;

    [Header("Colour Quantisation Settings")]
    [Range(2f, 32f)] public float redSteps = 8f;
    [Range(2f, 32f)] public float greenSteps = 8f;
    [Range(2f, 32f)] public float blueSteps = 8f;
    [Range(0f, 1f)] public float effectStrength = 1f;

    [Header("Dithering Settings")]
    [Range(0f, 1f)] public float ditherStrength = 1f;
    public BayerMatrixSize bayerMatrixSize = BayerMatrixSize.Bayer16x16;

    [Header("Matrix Mode")]
    public bool usePS1Matrix = false;

    [Header("Brightness Mode")]
    public bool usePerceivedBrightness = false;

    [Range(0.2f, 1.0f)] public float perceptualGamma = 0.5f;

    public enum BayerMatrixSize
    {
        Bayer2x2 = 2,
        Bayer4x4 = 4,
        Bayer8x8 = 8,
        Bayer16x16 = 16
    }

    private static readonly int RedStepsID = Shader.PropertyToID("_RedSteps");
    private static readonly int GreenStepsID = Shader.PropertyToID("_GreenSteps");
    private static readonly int BlueStepsID = Shader.PropertyToID("_BlueSteps");
    private static readonly int EffectStrengthID = Shader.PropertyToID("_EffectStrength");

    private static readonly int DitherStrengthID = Shader.PropertyToID("_DitherStrength");
    private static readonly int BayerSizeID = Shader.PropertyToID("_BayerSize");
    private static readonly int UsePS1MatrixID = Shader.PropertyToID("_UsePS1Matrix");

    private static readonly int UsePerceivedBrightnessID = Shader.PropertyToID("_UsePerceivedBrightness");
    private static readonly int PerceptualGammaID = Shader.PropertyToID("_PerceptualGamma");

    void Update()
    {
        if (quantisedDitherMaterial == null) return;

        quantisedDitherMaterial.SetFloat(RedStepsID, redSteps);
        quantisedDitherMaterial.SetFloat(GreenStepsID, greenSteps);
        quantisedDitherMaterial.SetFloat(BlueStepsID, blueSteps);
        quantisedDitherMaterial.SetFloat(EffectStrengthID, effectStrength);

        quantisedDitherMaterial.SetFloat(DitherStrengthID, ditherStrength);
        quantisedDitherMaterial.SetFloat(UsePS1MatrixID, usePS1Matrix ? 1.0f : 0.0f);

        if (!usePS1Matrix)
        {
            quantisedDitherMaterial.SetFloat(BayerSizeID, (float)bayerMatrixSize);
        }

        quantisedDitherMaterial.SetFloat(UsePerceivedBrightnessID, usePerceivedBrightness ? 1.0f : 0.0f);
        quantisedDitherMaterial.SetFloat(PerceptualGammaID, perceptualGamma);
    }
}