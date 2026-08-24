using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the CRT post-processing effect.
[Serializable, VolumeComponentMenu("Post-processing/CRT")]
public class CRTVolume : VolumeComponent
{
    [Tooltip("Controls how strongly the image curves toward the screen edges.")]
    public ClampedFloatParameter curvature = new ClampedFloatParameter(1.0f, 1.0f, 10.0f);

    [Tooltip("Controls how wide the vignette fade is at the edges.")]
    public ClampedFloatParameter vignetteWidth = new ClampedFloatParameter(30.0f, 1.0f, 100.0f);

    [Tooltip("How dark the gaps between scanline rows are. 0 = no scanlines, 1 = fully dark gaps.")]
    public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0.3f, 0f, 1f);

    [Tooltip("Number of scanlines across the screen height.")]
    public ClampedFloatParameter scanlineCount = new ClampedFloatParameter(300f, 50f, 1000f);

    [Tooltip("How rounded the screen corners are. 0 = sharp corners.")]
    public ClampedFloatParameter cornerRadius = new ClampedFloatParameter(0.05f, 0f, 0.2f);

    [Tooltip("How hard the transition from screen to black is at the corners.")]
    public ClampedFloatParameter cornerSharpness = new ClampedFloatParameter(20.0f, 1.0f, 100.0f);

    [Tooltip("How visible the RGB phosphor dot pattern is. 0 = no pattern.")]
    public ClampedFloatParameter phosphorIntensity = new ClampedFloatParameter(0.15f, 0f, 1f);
}