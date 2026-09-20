using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the Fear Vignette post-processing effect.
// Parameters are driven programmatically by FearPostProcessEffects at runtime.
[Serializable, VolumeComponentMenu("Post-processing/Fear Vignette")]
public class FearVignetteVolume : VolumeComponent
{
    [Tooltip("How far the vignette encroaches from the edges. 0 = none, 1 = full coverage.")]
    public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("How gradual the vignette falloff is. Lower values give a harder edge.")]
    public ClampedFloatParameter vignetteSoftness = new ClampedFloatParameter(0.3f, 0.01f, 1f);

    [Tooltip("The colour the vignette fades toward.")]
    public ColorParameter vignetteColor = new ColorParameter(Color.black);

    [Tooltip("How much the noise distorts the vignette edge.")]
    public ClampedFloatParameter noiseIntensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Size of the noise pattern. Higher values give finer, more detailed noise.")]
    public ClampedFloatParameter noiseScale = new ClampedFloatParameter(6f, 1f, 20f);

    [Tooltip("How fast the noise creeps inward.")]
    public FloatParameter noiseSpeed = new FloatParameter(0.3f);

    [Tooltip("How long in seconds before each noise layer resets.")]
    public FloatParameter cycleDuration = new FloatParameter(10f);

    [Tooltip("Blend mode: 0 = Multiply, 1 = Screen, 2 = Overlay, 3 = Hard Light.")]
    public ClampedIntParameter blendMode = new ClampedIntParameter(0, 0, 3);
}
