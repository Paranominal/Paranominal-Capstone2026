using UnityEngine;
using UnityEngine.Rendering;

// Summary: Handles fear-driven post-processing effects including vignette and chromatic aberration.
[ExecuteAlways]
public class FearPostProcessEffects : MonoBehaviour
{
    public enum VignetteBlendMode { Multiply, Screen, Overlay, HardLight }

    [Header("Fear Vignette")]
    [SerializeField] private Material vignetteMaterial;
    [Tooltip("The colour the vignette fades toward. Try dark colours with Multiply, lighter colours with Screen.")]
    [SerializeField] private Color vignetteColor = Color.black;
    [Tooltip("How the vignette colour blends with the scene. Multiply darkens, Screen lightens, Overlay adds contrast, Hard Light is a punchier Overlay.")]
    [SerializeField] private VignetteBlendMode blendMode = VignetteBlendMode.Multiply;
    [Tooltip("Global intensity of the vignette effect. Scales all vignette and noise values proportionally.")]
    [Range(0f, 1f)]
    [SerializeField] private float effectStrength = 1.0f;

    [Header("Advanced Settings")]
    [Tooltip("How far the vignette can encroach from the edges at maximum fear.")]
    [SerializeField] private float vignetteIntensityMax = 0.8f;
    [Tooltip("How gradual the vignette falloff is. Lower values give a harder edge.")]
    [SerializeField] private float vignetteSoftness = 0.3f;
    [Tooltip("How much the noise distorts the vignette edge at maximum fear.")]
    [SerializeField] private float noiseIntensityMax = 0.5f;
    [Tooltip("Size of the noise pattern. Higher values give finer, more detailed noise.")]
    [SerializeField] private float noiseScale = 6.0f;
    [Tooltip("How fast the noise creeps inward at minimum fear.")]
    [SerializeField] private float noiseSpeedMin = 0.1f;
    [Tooltip("How fast the noise creeps inward at maximum fear.")]
    [SerializeField] private float noiseSpeedMax = 0.5f;
    [Tooltip("How long in seconds before each noise layer resets. Shorter values reduce stretching but may show visible resets.")]
    [SerializeField] private float cycleDuration = 10f;

    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSoftnessID = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int VignetteColorID = Shader.PropertyToID("_VignetteColor");
    private static readonly int NoiseIntensityID = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int CycleDurationID = Shader.PropertyToID("_CycleDuration");
    private static readonly int BlendModeID = Shader.PropertyToID("_BlendMode");
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

        float strength = normalizedFear * effectStrength;

        float vignetteIntensity = vignetteIntensityMax * strength;
        float noiseIntensity = noiseIntensityMax * strength;
        float noiseSpeed = Mathf.Lerp(noiseSpeedMin, noiseSpeedMax, strength);

        vignetteMaterial.SetFloat(VignetteIntensityID, vignetteIntensity);
        vignetteMaterial.SetFloat(VignetteSoftnessID, vignetteSoftness);
        vignetteMaterial.SetColor(VignetteColorID, vignetteColor);
        vignetteMaterial.SetFloat(NoiseIntensityID, noiseIntensity);
        vignetteMaterial.SetFloat(NoiseScaleID, noiseScale);
        vignetteMaterial.SetFloat(NoiseSpeedID, noiseSpeed);
        vignetteMaterial.SetFloat(CycleDurationID, cycleDuration);
        vignetteMaterial.SetFloat(BlendModeID, (float)blendMode);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle any rank-threshold post-process changes (chromatic aberration etc.)
    }
}