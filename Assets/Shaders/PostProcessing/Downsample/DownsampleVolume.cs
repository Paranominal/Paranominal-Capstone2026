using System;
using UnityEngine;
using UnityEngine.Rendering;

// Summary: Volume component for the Downsample (pixelate) post-processing effect.
[Serializable, VolumeComponentMenu("Post-processing/Downsample")]
public class DownsampleVolume : VolumeComponent
{
    [Tooltip("Size of each pixel block in screen pixels. Higher values give a chunkier pixelated look.")]
    public ClampedFloatParameter pixelSize = new ClampedFloatParameter(4f, 1f, 512f);
}
