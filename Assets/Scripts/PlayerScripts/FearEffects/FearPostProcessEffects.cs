using UnityEngine;
using UnityEngine.Rendering;

// Summary: Handles fear-driven post-processing effects by writing to the FearVignetteVolume on a Volume Profile.
public class FearPostProcessEffects : MonoBehaviour
{
    public enum VignetteBlendMode { Multiply, Screen, Overlay, HardLight }

    [Header("Fear Vignette")]
    [Tooltip("The Volume component that holds the Fear Vignette override.")]
    [SerializeField] private Volume postProcessVolume;
    [Tooltip("The colour the vignette fades toward.")]
    [SerializeField] private Color vignetteColor = Color.black;
    [Tooltip("How the vignette colour blends with the scene.")]
    [SerializeField] private VignetteBlendMode blendMode = VignetteBlendMode.Multiply;
    [Tooltip("Global intensity of the vignette effect.")]
    [Range(0f, 1f)]
    [SerializeField] private float effectStrength = 1.0f;

    [Header("Advanced Settings")]
    [Tooltip("How far the vignette can encroach from the edges at maximum fear.")]
    [SerializeField] private float vignetteIntensityMax = 0.8f;
    [Tooltip("How gradual the vignette falloff is.")]
    [SerializeField] private float vignetteSoftness = 0.3f;
    [Tooltip("How much the noise distorts the vignette edge at maximum fear.")]
    [SerializeField] private float noiseIntensityMax = 0.5f;
    [Tooltip("Size of the noise pattern.")]
    [SerializeField] private float noiseScale = 6.0f;
    [Tooltip("How fast the noise creeps inward at minimum fear.")]
    [SerializeField] private float noiseSpeedMin = 0.1f;
    [Tooltip("How fast the noise creeps inward at maximum fear.")]
    [SerializeField] private float noiseSpeedMax = 0.5f;
    [Tooltip("How long in seconds before each noise layer resets.")]
    [SerializeField] private float cycleDuration = 10f;

    private FearVignetteVolume vignetteVolume;

    private void Start()
    {
        TryGetVolumeComponent();
    }

    private void TryGetVolumeComponent()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignetteVolume);
        }
    }

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        if (vignetteVolume == null)
        {
            TryGetVolumeComponent();
            if (vignetteVolume == null) return;
        }

        float strength = normalizedFear * effectStrength;

        vignetteVolume.vignetteIntensity.Override(vignetteIntensityMax * strength);
        vignetteVolume.noiseIntensity.Override(noiseIntensityMax * strength);
        vignetteVolume.noiseSpeed.Override(Mathf.Lerp(noiseSpeedMin, noiseSpeedMax, strength));
        vignetteVolume.vignetteSoftness.Override(vignetteSoftness);
        vignetteVolume.vignetteColor.Override(vignetteColor);
        vignetteVolume.noiseScale.Override(noiseScale);
        vignetteVolume.cycleDuration.Override(cycleDuration);
        vignetteVolume.blendMode.Override((int)blendMode);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle any rank-threshold post-process changes (chromatic aberration etc.)
    }

    private void OnDisable()
    {
        if (vignetteVolume == null) return;
        vignetteVolume.vignetteIntensity.Override(0f);
        vignetteVolume.noiseIntensity.Override(0f);
    }
}