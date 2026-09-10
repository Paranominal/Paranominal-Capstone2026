using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the Sharpen post-processing effect. Add this as an override on a Volume to control sharpness settings.
[Serializable, VolumeComponentMenu("Post-processing/Sharpen")]
public class SharpenVolume : VolumeComponent
{
    [Tooltip("Controls how strongly edges are enhanced.")]
    public ClampedFloatParameter sharpness = new ClampedFloatParameter(0.25f, 0f, 5f);

    [Tooltip("Controls how far away neighbouring pixels are sampled.")]
    public ClampedFloatParameter sampleDistance = new ClampedFloatParameter(1.0f, 0.25f, 3f);
}
