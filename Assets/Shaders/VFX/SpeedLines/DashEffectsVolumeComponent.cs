// Summary: Volume Component for the dash post-processing effects.
// Exposes toggleable radial blur, UV warp, and action lines with a shared center mask.
// Intensity is driven at runtime by PlayerDash during the dash fade in/out.

using UnityEngine;
using UnityEngine.Rendering;

[VolumeComponentMenu("Custom Post-Processing/Dash Effects")]
public class DashEffectsVolumeComponent : VolumeComponent
{
    [Tooltip("Overall effect intensity. 0 = no effect, 1 = full strength. Driven by PlayerDash at runtime.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Header("Radial Blur")]
    [Tooltip("Enable the radial zoom blur layer.")]
    public BoolParameter enableBlur = new BoolParameter(true);

    [Tooltip("Number of samples along the radial direction. Higher = smoother but more expensive.")]
    public ClampedFloatParameter sampleCount = new ClampedFloatParameter(8f, 4f, 16f);

    [Tooltip("How far the blur stretches outward from each pixel.")]
    public ClampedFloatParameter blurStrength = new ClampedFloatParameter(0.1f, 0f, 0.5f);

    [Tooltip("Distance from screen center where the blur begins. Lower values blur closer to center.")]
    public ClampedFloatParameter centerFalloff = new ClampedFloatParameter(0.3f, 0f, 1f);

    [Header("Warp Distortion")]
    [Tooltip("Enable the radial UV warp layer.")]
    public BoolParameter enableWarp = new BoolParameter(true);

    [Tooltip("How much the screen stretches outward from center. Negative values pull inward.")]
    public ClampedFloatParameter warpStrength = new ClampedFloatParameter(0.3f, -3f, 3f);

    [Header("Action Lines")]
    [Tooltip("Enable the animated action lines layer.")]
    public BoolParameter enableLines = new BoolParameter(true);

    [Tooltip("Colour and alpha of the speed lines. Alpha controls blend strength.")]
    public ColorParameter linesColour = new ColorParameter(Color.white);

    [Tooltip("Number of angular divisions for the noise pattern. Higher = more lines.")]
    public ClampedFloatParameter linesTiling = new ClampedFloatParameter(200f, 10f, 500f);

    [Tooltip("How far the noise stretches along the radial axis. Lower = longer streaks.")]
    public ClampedFloatParameter linesRadialScale = new ClampedFloatParameter(0.1f, 0f, 10f);

    [Tooltip("Sharpens the noise into distinct lines. Higher = thinner, sharper lines.")]
    public ClampedFloatParameter linesPower = new ClampedFloatParameter(1f, 0.1f, 10f);

    [Tooltip("Threshold cutoff. Values below this are removed. Higher = fewer, brighter lines.")]
    public ClampedFloatParameter linesRemap = new ClampedFloatParameter(0.8f, 0f, 1f);

    [Tooltip("Speed of the radial scroll animation.")]
    public ClampedFloatParameter linesAnimation = new ClampedFloatParameter(3f, 0f, 20f);

    [Header("Center Mask")]
    [Tooltip("Size of the clear area at screen center. Higher = larger clear zone.")]
    public ClampedFloatParameter maskScale = new ClampedFloatParameter(1f, 0f, 2f);

    [Tooltip("How sharp the mask edge is. 0 = soft gradient, 1 = hard cutoff.")]
    public ClampedFloatParameter maskHardness = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Controls the mask falloff curve. Higher = steeper transition.")]
    public ClampedFloatParameter maskPower = new ClampedFloatParameter(5f, 0.1f, 20f);

    public bool IsActive() => intensity.value > 0f;
}