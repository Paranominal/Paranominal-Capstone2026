// Summary: Volume Component for the Special Shot impact frame.
// Four layers that can be toggled independently: a solid flash, a two-tone thresholded scene,
// radial speed lines and UV jitter, all centred on a focal point.
// The layer toggles, focal point and seed are driven per frame at runtime by ImpactFrameController.

using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum ImpactFlashColour { Light, Dark }

// Summary: Volume parameter wrapper so the flash colour shows as a dropdown.
[Serializable]
public sealed class ImpactFlashColourParameter : VolumeParameter<ImpactFlashColour>
{
    public ImpactFlashColourParameter(ImpactFlashColour value, bool overrideState = false) : base(value, overrideState) { }
}

[VolumeComponentMenu("Custom Post-Processing/Impact Frame")]
public class ImpactFrameVolumeComponent : VolumeComponent
{
    [Header("Layers (driven at runtime)")]
    [Tooltip("Solid fill of the flash colour. Overrides every other layer while on.")]
    public BoolParameter flashActive = new BoolParameter(false);

    [Tooltip("Two-tone thresholded scene.")]
    public BoolParameter sceneActive = new BoolParameter(false);

    [Tooltip("Radial speed lines. Invert the two-tone scene, or draw solid light over the normal scene.")]
    public BoolParameter linesActive = new BoolParameter(false);

    [Tooltip("Pushes angular segments of the scene outward from the focal point.")]
    public BoolParameter jitterActive = new BoolParameter(false);

    [Header("Runtime")]
    [Tooltip("Screen-space centre of the effect (0 to 1). Set to the destroyed weakpoint's screen position at runtime.")]
    public Vector2Parameter focalPoint = new Vector2Parameter(new Vector2(0.5f, 0.5f));

    [Tooltip("Randomises the line and jitter pattern. Re-rolled on every trigger.")]
    public ClampedFloatParameter seed = new ClampedFloatParameter(0f, 0f, 100f);

    [Header("Colours")]
    [Tooltip("Colour used for the dark side of the mask.")]
    public ColorParameter darkColour = new ColorParameter(Color.black);

    [Tooltip("Colour used for the light side of the mask and for lines over the normal scene.")]
    public ColorParameter lightColour = new ColorParameter(Color.white);

    [Tooltip("Which colour the flash layer fills the screen with.")]
    public ImpactFlashColourParameter flashColour = new ImpactFlashColourParameter(ImpactFlashColour.Light);

    [Header("Scene Threshold")]
    [Tooltip("Brightness cutoff between dark and light. Higher = more of the scene goes dark.")]
    public ClampedFloatParameter sceneThreshold = new ClampedFloatParameter(0.5f, -1f, 2f);

    [Tooltip("How much the upward-facing normals brighten the scene. Adds shape definition to geometry. Sprites don't write normals.")]
    public ClampedFloatParameter normalsWeight = new ClampedFloatParameter(1f, 0f, 2f);

    [Header("Speed Lines")]
    [Tooltip("Number of angular divisions. Higher = more, thinner lines.")]
    public ClampedFloatParameter linesTiling = new ClampedFloatParameter(250f, 10f, 500f);

    [Tooltip("Noise scale along each line. Higher = lines break up into shorter segments.")]
    public ClampedFloatParameter linesNoiseScale = new ClampedFloatParameter(6f, 1f, 30f);

    [Tooltip("Line cutoff. Higher = fewer lines.")]
    public ClampedFloatParameter linesThreshold = new ClampedFloatParameter(0.4f, 0f, 1f);

    [Tooltip("Radius around the focal point kept clear of lines.")]
    public ClampedFloatParameter linesClearRadius = new ClampedFloatParameter(0.03f, 0f, 1f);

    [Header("UV Jitter")]
    [Tooltip("Number of angular jitter segments.")]
    public ClampedFloatParameter jitterScale = new ClampedFloatParameter(40f, 1f, 300f);

    [Tooltip("Jitter cutoff. Higher = fewer segments get pushed.")]
    public ClampedFloatParameter jitterThreshold = new ClampedFloatParameter(0.5f, 0f, 1f);

    [Tooltip("How far jittered segments are pushed outward from the focal point.")]
    public ClampedFloatParameter jitterStrength = new ClampedFloatParameter(0.04f, 0f, 0.1f);

    public bool IsActive() => flashActive.value || sceneActive.value || linesActive.value || jitterActive.value;
}
