using UnityEngine;
using UnityEngine.Rendering;

// Summary: Handles fear-driven post-processing effects including vignette and chromatic aberration.
[ExecuteAlways]
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
    [SerializeField] private Color vignetteColor = Color.black;
    [SerializeField] private float vignetteSoftness = 0.3f;
    [SerializeField] private float noiseScale = 6.0f;
    [SerializeField] private float noiseSpeed = 0.3f;

    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSoftnessID = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int VignetteColorID = Shader.PropertyToID("_VignetteColor");
    private static readonly int NoiseIntensityID = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int EnabledID = Shader.PropertyToID("_Enabled");

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (vignetteMaterial == null) return;

        vignetteMaterial.SetFloat(EnabledID, cam.cameraType == CameraType.SceneView ? 0f : 1f);
    }

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        if (vignetteMaterial == null) return;

        float vignetteIntensity = Mathf.Lerp(vignetteIntensityMin, vignetteIntensityMax, normalizedFear);
        float noiseIntensity = Mathf.Lerp(noiseIntensityMin, noiseIntensityMax, normalizedFear);

        vignetteMaterial.SetFloat(VignetteIntensityID, vignetteIntensity);
        vignetteMaterial.SetFloat(VignetteSoftnessID, vignetteSoftness);
        vignetteMaterial.SetColor(VignetteColorID, vignetteColor);
        vignetteMaterial.SetFloat(NoiseIntensityID, noiseIntensity);
        vignetteMaterial.SetFloat(NoiseScaleID, noiseScale);
        vignetteMaterial.SetFloat(NoiseSpeedID, noiseSpeed);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle any rank-threshold post-process changes (chromatic aberration etc.)
    }
}