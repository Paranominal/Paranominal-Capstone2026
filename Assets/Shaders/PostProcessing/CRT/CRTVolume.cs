using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the CRT post-processing effect.
[Serializable, VolumeComponentMenu("Custom Post-Processing/CRT")]
public class CRTVolume : VolumeComponent
{
    [Tooltip("Texel distance for the soft blur samples. 0 = no blur.")]
    public ClampedFloatParameter blurOffset = new ClampedFloatParameter(1.0f, 0.0f, 3.0f);

    [Tooltip("How dark the gaps between scanline rows are. 0 = no scanlines.")]
    public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);

    [Tooltip("Number of scanlines across the screen height.")]
    public ClampedFloatParameter scanlineCount = new ClampedFloatParameter(300f, 50f, 1000f);

    [Tooltip("How fast the scanlines scroll vertically.")]
    public ClampedFloatParameter scanlineSpeed = new ClampedFloatParameter(0.5f, 0.0f, 5.0f);

    [Tooltip("Strength of the edge vignette darkening.")]
    public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);

    [Tooltip("How gradual the vignette falloff is.")]
    public ClampedFloatParameter vignetteSmoothness = new ClampedFloatParameter(0.3f, 0.01f, 1.0f);

    [Tooltip("Enable the RGB phosphor dot pattern.")]
    public BoolParameter usePhosphor = new BoolParameter(false);

    [Tooltip("How visible the RGB phosphor dot pattern is. 0 = no pattern.")]
    public ClampedFloatParameter phosphorIntensity = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);

    [Tooltip("Pixel width of each phosphor column. Higher values reduce moire with dithering.")]
    public ClampedFloatParameter phosphorScale = new ClampedFloatParameter(2.0f, 1.0f, 6.0f);
}