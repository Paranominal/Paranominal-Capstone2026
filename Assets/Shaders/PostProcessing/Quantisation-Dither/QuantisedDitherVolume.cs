using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the Quantised Dither post-processing effect.
[Serializable, VolumeComponentMenu("Post-processing/Quantised Dither")]
public class QuantisedDitherVolume : VolumeComponent
{
    [Tooltip("Number of quantisation levels for the red channel.")]
    public ClampedFloatParameter redSteps = new ClampedFloatParameter(8f, 2f, 32f);

    [Tooltip("Number of quantisation levels for the green channel.")]
    public ClampedFloatParameter greenSteps = new ClampedFloatParameter(8f, 2f, 32f);

    [Tooltip("Number of quantisation levels for the blue channel.")]
    public ClampedFloatParameter blueSteps = new ClampedFloatParameter(8f, 2f, 32f);

    [Tooltip("Blend between the original and quantised image.")]
    public ClampedFloatParameter effectStrength = new ClampedFloatParameter(1f, 0f, 1f);

    [Tooltip("How strongly the dither pattern is applied.")]
    public ClampedFloatParameter ditherStrength = new ClampedFloatParameter(1f, 0f, 1f);

    [Tooltip("Size of the Bayer matrix used for dithering. Ignored when using PS1 matrix.")]
    public ClampedFloatParameter bayerSize = new ClampedFloatParameter(16f, 2f, 16f);

    [Tooltip("Use the PS1-style dither matrix instead of standard Bayer.")]
    public BoolParameter usePS1Matrix = new BoolParameter(false);

    [Tooltip("Use perceived brightness for dithering instead of linear luminance.")]
    public BoolParameter usePerceivedBrightness = new BoolParameter(false);

    [Tooltip("Gamma curve applied when using perceived brightness mode.")]
    public ClampedFloatParameter perceptualGamma = new ClampedFloatParameter(0.5f, 0.2f, 1.0f);
}
