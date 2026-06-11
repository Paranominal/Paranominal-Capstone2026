using UnityEngine;

// Summary: Handles fear-driven post-processing effects including vignette and chromatic aberration.
public class FearPostProcessEffects : MonoBehaviour
{
    [Header("Fear Vignette")]
    [SerializeField] private Material vignetteMaterial;

    [Header("Vignette Intensity")]
    [SerializeField] private float vignetteIntensityMin = 0.0f;
    [SerializeField] private float vignetteIntensityMax = 0.8f;

    [Header("Vignette Noise")]
    [SerializeField] private float noiseIntensityMin = 0.0f;
    [SerializeField] private float noiseIntensityMax = 0.5f;

    [Header("Vignette Settings")]
    [SerializeField] private float vignetteSoftness = 0.3f;
    [SerializeField] private float noiseScale = 6.0f;
    [SerializeField] private float noiseSpeed = 0.3f;

    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSoftnessID = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int NoiseIntensityID = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        if (vignetteMaterial == null) return;

        float vignetteIntensity = Mathf.Lerp(vignetteIntensityMin, vignetteIntensityMax, normalizedFear);
        float noiseIntensity = Mathf.Lerp(noiseIntensityMin, noiseIntensityMax, normalizedFear);

        vignetteMaterial.SetFloat(VignetteIntensityID, vignetteIntensity);
        vignetteMaterial.SetFloat(VignetteSoftnessID, vignetteSoftness);
        vignetteMaterial.SetFloat(NoiseIntensityID, noiseIntensity);
        vignetteMaterial.SetFloat(NoiseScaleID, noiseScale);
        vignetteMaterial.SetFloat(NoiseSpeedID, noiseSpeed);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle any rank-threshold post-process changes (chromatic aberration etc.)
    }
}