// Summary: Holds the player's graphics settings and applies them. Post-processing settings are applied through a
// runtime-only override Volume (created on a child of this object) that sits above the scene's Global Volume.
// Overrides start off, so untouched settings pass the designer's profile values straight through.
// Settings driven by other scripts (Fear Vignette, Dash intensity, Impact Frames, camera effects) are exposed as
// properties those scripts read. Settings reset to the designer's values on each launch.
// EDIT (ui-render-target): designer values are now read from a private volume stack evaluated against the world volumes only,
// so they always come from the Global Volume's profile, whichever camera rendered last.
// Lives on GameSystems (persistent).

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// On/off settings shown as toggle rows.
public enum GraphicsToggle
{
    DisableAllPostProcessing,
    Bloom,
    ChromaticAberration,
    Dithering,
    Sharpening,
    CRTBlur,
    CRTScanlines,
    CRTVignette,
    CRTPhosphor,
    ImpactFrames,
    DashEthereal,
    DashBlur,
    DashDistortion,
    DashActionLines,
    CameraShake,
    ViewBobbing,
    WeaponSway
}

// Value settings shown as slider rows.
public enum GraphicsSlider
{
    ChromaticAberrationIntensity,
    DownsamplePixelSize,
    ColourSteps,
    SharpenSampleDistance,
    FearVignetteIntensity,
    DashIntensity
}

// Dither patterns shown as radio-style tick-boxes.
public enum DitherPattern
{
    TwoByTwo,
    FourByFour,
    EightByEight,
    SixteenBySixteen,
    PS1
}

public class GraphicsSettingsManager : MonoBehaviour
{
    public static GraphicsSettingsManager Instance { get; private set; }

    [Header("Override Volume")]
    // EDIT (ui-render-target): should now be a dedicated layer, so the settings reach the UI camera without the Global Volume coming with them.
    [Tooltip("Dedicated layer for the override volume. Must be in the Volume Mask of every camera the settings should affect (PlayerCam and UICam), " +
             "and NOT in Designer Volume Mask.")]
    [SerializeField] private string volumeLayerName = "Default";
    [Tooltip("Must be higher than every scene volume so the player's settings win.")]
    [SerializeField] private float volumePriority = 100f;

    // EDIT (ui-render-target)
    [Header("Designer Values")]
    [Tooltip("Volume layers the menu reads its default values from, usually just the Global Volume's layer. Don't include the override volume's layer.")]
    [SerializeField] private LayerMask designerVolumeMask = 1;

    [Header("Slider Ranges")]
    [Tooltip("Highest Downsample pixel size the slider allows.")]
    [SerializeField, Min(2)] private int maxPixelSize = 8;

    // Fires after any setting changes. UI rows use it to refresh values and greyed-out states.
    public event Action OnSettingsChanged;

    // Colour step options for the stepped slider. The slider value is an index into this.
    private static readonly int[] colourStepOptions = { 4, 8, 16, 32 };

    // ---- Player state. Null means "not touched", so the designer's value is used. ----

    private bool disableAll;

    private bool? bloomOn;
    private bool? chromaticOn;
    private float? chromaticIntensity;
    private int? pixelSize;
    private int? colourStepIndex;
    private bool? ditherOn;
    private DitherPattern? ditherPattern;
    private bool? sharpenOn;
    private float? sampleDistance;
    private bool? crtBlurOn;
    private bool? crtScanlinesOn;
    private bool? crtVignetteOn;
    private bool? crtPhosphorOn;

    private bool? dashEtherealOn;
    private bool? dashBlurOn;
    private bool? dashDistortionOn;
    private bool? dashLinesOn;

    // ---- Script-driven settings, read by other systems. ----

    public bool ImpactFramesEnabled { get; private set; } = true;
    public bool CameraShakeEnabled { get; private set; } = true;
    public bool ViewBobbingEnabled { get; private set; } = true;
    public bool WeaponSwayEnabled { get; private set; } = true;
    // 0 to 1 multipliers. 0 disables the effect.
    public float FearVignetteMultiplier { get; private set; } = 1f;
    public float DashIntensityMultiplier { get; private set; } = 1f;

    // The runtime override volume. Scripts that search the scene for a Volume should skip this one.
    public Volume OverrideVolume => overrideVolume;

    // ---- Designer values, cached from the resolved volume stack while our override is off. ----

    private float designBloomIntensity = 1f;
    private float designChromaticIntensity;
    private float designPixelSize = 1f;
    private float designColourSteps = 8f;
    private float designDitherStrength = 1f;
    private BayerMatrixSize designBayerSize = BayerMatrixSize.SixteenBySixteen;
    private bool designUsePS1;
    private float designSharpness;
    private float designSampleDistance = 1f;
    private float designBlurOffset;
    private float designScanlineIntensity;
    private float designCrtVignetteIntensity;
    private bool designUsePhosphor;
    private bool designDashEthereal = true;
    private bool designDashBlur = true;
    private bool designDashWarp = true;
    private bool designDashLines = true;

    // ---- Override volume. ----

    private Volume overrideVolume;
    private VolumeProfile overrideProfile;
    private Bloom bloom;
    private ChromaticAberration chromatic;
    private DownsampleVolume downsample;
    private QuantisedDitherVolume quantise;
    private SharpenVolume sharpen;
    private CRTVolume crt;
    private DashEffectsVolumeComponent dash;

    // EDIT (ui-render-target): replaces the per-frame override tracking. Evaluated only against designerVolumeMask,
    // so it never contains our overrides or the UI volume.
    private VolumeStack designerStack;
    private int designerRefreshFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        CreateOverrideVolume();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        // Runtime profile isn't an asset, so it has to be cleaned up by hand.
        if (overrideProfile != null)
            Destroy(overrideProfile);

        // EDIT (ui-render-target)
        if (designerStack != null && VolumeManager.instance != null)
            VolumeManager.instance.DestroyStack(designerStack);

        Instance = null;
    }

    // Summary: Builds the child Volume and its runtime profile. Components are added with every override off.
    private void CreateOverrideVolume()
    {
        GameObject volumeObject = new GameObject("GraphicsSettingsVolume");
        volumeObject.transform.SetParent(transform, false);

        int layer = LayerMask.NameToLayer(volumeLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"GraphicsSettingsManager: layer \"{volumeLayerName}\" not found, using Default.");
            layer = 0;
        }
        volumeObject.layer = layer;

        overrideProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        overrideProfile.name = "GraphicsSettingsProfile (Runtime)";

        bloom = overrideProfile.Add<Bloom>(false);
        chromatic = overrideProfile.Add<ChromaticAberration>(false);
        downsample = overrideProfile.Add<DownsampleVolume>(false);
        quantise = overrideProfile.Add<QuantisedDitherVolume>(false);
        sharpen = overrideProfile.Add<SharpenVolume>(false);
        crt = overrideProfile.Add<CRTVolume>(false);
        dash = overrideProfile.Add<DashEffectsVolumeComponent>(false);

        overrideVolume = volumeObject.AddComponent<Volume>();
        overrideVolume.isGlobal = true;
        overrideVolume.priority = volumePriority;
        overrideVolume.weight = 1f;
        overrideVolume.sharedProfile = overrideProfile;
    }

    // ---- Public API for the UI rows ----

    public bool GetToggle(GraphicsToggle setting)
    {
        RefreshDesignerValues();

        switch (setting)
        {
            case GraphicsToggle.DisableAllPostProcessing: return disableAll;
            case GraphicsToggle.Bloom: return bloomOn ?? designBloomIntensity > 0f;
            case GraphicsToggle.ChromaticAberration: return chromaticOn ?? designChromaticIntensity > 0f;
            case GraphicsToggle.Dithering: return ditherOn ?? designDitherStrength > 0f;
            case GraphicsToggle.Sharpening: return sharpenOn ?? designSharpness > 0f;
            case GraphicsToggle.CRTBlur: return crtBlurOn ?? designBlurOffset > 0f;
            case GraphicsToggle.CRTScanlines: return crtScanlinesOn ?? designScanlineIntensity > 0f;
            case GraphicsToggle.CRTVignette: return crtVignetteOn ?? designCrtVignetteIntensity > 0f;
            case GraphicsToggle.CRTPhosphor: return crtPhosphorOn ?? designUsePhosphor;
            case GraphicsToggle.ImpactFrames: return ImpactFramesEnabled;
            case GraphicsToggle.DashEthereal: return dashEtherealOn ?? designDashEthereal;
            case GraphicsToggle.DashBlur: return dashBlurOn ?? designDashBlur;
            case GraphicsToggle.DashDistortion: return dashDistortionOn ?? designDashWarp;
            case GraphicsToggle.DashActionLines: return dashLinesOn ?? designDashLines;
            case GraphicsToggle.CameraShake: return CameraShakeEnabled;
            case GraphicsToggle.ViewBobbing: return ViewBobbingEnabled;
            case GraphicsToggle.WeaponSway: return WeaponSwayEnabled;
        }
        return false;
    }

    public void SetToggle(GraphicsToggle setting, bool value)
    {
        RefreshDesignerValues();

        switch (setting)
        {
            case GraphicsToggle.DisableAllPostProcessing: disableAll = value; break;
            case GraphicsToggle.Bloom: bloomOn = value; break;
            case GraphicsToggle.ChromaticAberration: chromaticOn = value; break;
            case GraphicsToggle.Dithering: ditherOn = value; break;
            case GraphicsToggle.Sharpening: sharpenOn = value; break;
            case GraphicsToggle.CRTBlur: crtBlurOn = value; break;
            case GraphicsToggle.CRTScanlines: crtScanlinesOn = value; break;
            case GraphicsToggle.CRTVignette: crtVignetteOn = value; break;
            case GraphicsToggle.CRTPhosphor: crtPhosphorOn = value; break;
            case GraphicsToggle.ImpactFrames: ImpactFramesEnabled = value; break;
            case GraphicsToggle.DashEthereal: dashEtherealOn = value; break;
            case GraphicsToggle.DashBlur: dashBlurOn = value; break;
            case GraphicsToggle.DashDistortion: dashDistortionOn = value; break;
            case GraphicsToggle.DashActionLines: dashLinesOn = value; break;
            case GraphicsToggle.CameraShake: CameraShakeEnabled = value; break;
            case GraphicsToggle.ViewBobbing: ViewBobbingEnabled = value; break;
            case GraphicsToggle.WeaponSway: WeaponSwayEnabled = value; break;
        }

        ApplyAndNotify();
    }

    public float GetSlider(GraphicsSlider setting)
    {
        RefreshDesignerValues();

        switch (setting)
        {
            case GraphicsSlider.ChromaticAberrationIntensity: return chromaticIntensity ?? designChromaticIntensity;
            case GraphicsSlider.DownsamplePixelSize: return pixelSize ?? Mathf.Clamp(Mathf.RoundToInt(designPixelSize), 1, maxPixelSize);
            case GraphicsSlider.ColourSteps: return colourStepIndex ?? NearestColourStepIndex(designColourSteps);
            case GraphicsSlider.SharpenSampleDistance: return sampleDistance ?? designSampleDistance;
            case GraphicsSlider.FearVignetteIntensity: return FearVignetteMultiplier;
            case GraphicsSlider.DashIntensity: return DashIntensityMultiplier;
        }
        return 0f;
    }

    public void SetSlider(GraphicsSlider setting, float value)
    {
        RefreshDesignerValues();

        switch (setting)
        {
            case GraphicsSlider.ChromaticAberrationIntensity: chromaticIntensity = Mathf.Clamp01(value); break;
            case GraphicsSlider.DownsamplePixelSize: pixelSize = Mathf.Clamp(Mathf.RoundToInt(value), 1, maxPixelSize); break;
            case GraphicsSlider.ColourSteps: colourStepIndex = Mathf.Clamp(Mathf.RoundToInt(value), 0, colourStepOptions.Length - 1); break;
            case GraphicsSlider.SharpenSampleDistance: sampleDistance = value; break;
            case GraphicsSlider.FearVignetteIntensity: FearVignetteMultiplier = Mathf.Clamp01(value); break;
            case GraphicsSlider.DashIntensity: DashIntensityMultiplier = Mathf.Clamp01(value); break;
        }

        ApplyAndNotify();
    }

    // Summary: Slider range for a setting. Rows call this to configure their Slider.
    public void GetSliderRange(GraphicsSlider setting, out float min, out float max, out bool wholeNumbers)
    {
        switch (setting)
        {
            case GraphicsSlider.DownsamplePixelSize:
                min = 1f; max = maxPixelSize; wholeNumbers = true; return;
            case GraphicsSlider.ColourSteps:
                min = 0f; max = colourStepOptions.Length - 1; wholeNumbers = true; return;
            case GraphicsSlider.SharpenSampleDistance:
                // Matches SharpenVolume's sampleDistance range.
                min = 0f; max = 3f; wholeNumbers = true; return;
            default:
                min = 0f; max = 1f; wholeNumbers = false; return;
        }
    }

    // Summary: Value label for a slider row.
    public string FormatSlider(GraphicsSlider setting, float value)
    {
        switch (setting)
        {
            case GraphicsSlider.DownsamplePixelSize:
                int size = Mathf.RoundToInt(value);
                return size <= 1 ? "0x" : $"{size}x";
            case GraphicsSlider.ColourSteps:
                int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, colourStepOptions.Length - 1);
                return colourStepOptions[index].ToString();
            case GraphicsSlider.SharpenSampleDistance:
                return Mathf.RoundToInt(value).ToString();
            default:
                return $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    public DitherPattern GetDitherPattern()
    {
        RefreshDesignerValues();

        if (ditherPattern.HasValue) return ditherPattern.Value;
        if (designUsePS1) return DitherPattern.PS1;

        switch (designBayerSize)
        {
            case BayerMatrixSize.TwoByTwo: return DitherPattern.TwoByTwo;
            case BayerMatrixSize.FourByFour: return DitherPattern.FourByFour;
            case BayerMatrixSize.EightByEight: return DitherPattern.EightByEight;
            default: return DitherPattern.SixteenBySixteen;
        }
    }

    public void SetDitherPattern(DitherPattern pattern)
    {
        RefreshDesignerValues();
        ditherPattern = pattern;
        ApplyAndNotify();
    }

    // Summary: Whether a control should be usable. Post-processing controls grey out under Disable All,
    // and sliders grey out while their own toggle is off.
    public bool IsInteractable(GraphicsToggle setting)
    {
        switch (setting)
        {
            case GraphicsToggle.Bloom:
            case GraphicsToggle.ChromaticAberration:
            case GraphicsToggle.Dithering:
            case GraphicsToggle.Sharpening:
            case GraphicsToggle.CRTBlur:
            case GraphicsToggle.CRTScanlines:
            case GraphicsToggle.CRTVignette:
            case GraphicsToggle.CRTPhosphor:
                return !disableAll;
            case GraphicsToggle.DashEthereal:
            case GraphicsToggle.DashBlur:
            case GraphicsToggle.DashDistortion:
            case GraphicsToggle.DashActionLines:
                return DashIntensityMultiplier > 0f;
            default:
                return true;
        }
    }

    public bool IsInteractable(GraphicsSlider setting)
    {
        switch (setting)
        {
            case GraphicsSlider.ChromaticAberrationIntensity: return !disableAll && GetToggle(GraphicsToggle.ChromaticAberration);
            case GraphicsSlider.DownsamplePixelSize: return !disableAll;
            case GraphicsSlider.ColourSteps: return !disableAll;
            case GraphicsSlider.SharpenSampleDistance: return !disableAll && GetToggle(GraphicsToggle.Sharpening);
            default: return true;
        }
    }

    public bool IsDitherPatternInteractable()
    {
        return !disableAll && GetToggle(GraphicsToggle.Dithering);
    }

    // ---- Applying ----

    private void ApplyAndNotify()
    {
        ApplyOverrides();
        OnSettingsChanged?.Invoke();
    }

    // Summary: Rebuilds every override from the current state. "Off" forces the parameter to its off value,
    // a player value overrides it, and an untouched setting releases the override so the designer's value shows.
    private void ApplyOverrides()
    {
        if (overrideProfile == null) return;

        bool pp = !disableAll;

        // Bloom
        SetFloat(bloom.intensity, !(pp && GetToggle(GraphicsToggle.Bloom)), 0f);

        // Chromatic Aberration
        if (!(pp && GetToggle(GraphicsToggle.ChromaticAberration)))
            SetFloat(chromatic.intensity, true, 0f);
        else
            SetFloat(chromatic.intensity, chromaticIntensity.HasValue, chromaticIntensity ?? 0f);

        // Downsample (1 = no downsampling)
        if (!pp)
            SetFloat(downsample.pixelSize, true, 1f);
        else
            SetFloat(downsample.pixelSize, pixelSize.HasValue, pixelSize ?? 1);

        // Colour Quantisation. Disable All turns off the whole shader, which includes dither.
        SetFloat(quantise.effectStrength, !pp, 0f);
        bool stepsSet = pp && colourStepIndex.HasValue;
        float steps = colourStepIndex.HasValue ? colourStepOptions[colourStepIndex.Value] : 8f;
        SetFloat(quantise.redSteps, stepsSet, steps);
        SetFloat(quantise.greenSteps, stepsSet, steps);
        SetFloat(quantise.blueSteps, stepsSet, steps);

        // Dithering
        SetFloat(quantise.ditherStrength, !(pp && GetToggle(GraphicsToggle.Dithering)), 0f);
        if (ditherPattern.HasValue)
        {
            bool usePS1 = ditherPattern.Value == DitherPattern.PS1;
            SetBool(quantise.usePS1Matrix, true, usePS1);

            // PS1 ignores the Bayer size, so it's only overridden for Bayer patterns.
            SetOverrideState(quantise.bayerSize, !usePS1);
            if (!usePS1)
                quantise.bayerSize.value = ToBayerSize(ditherPattern.Value);
        }
        else
        {
            SetOverrideState(quantise.usePS1Matrix, false);
            SetOverrideState(quantise.bayerSize, false);
        }

        // Sharpening
        SetFloat(sharpen.sharpness, !(pp && GetToggle(GraphicsToggle.Sharpening)), 0f);
        SetFloat(sharpen.sampleDistance, sampleDistance.HasValue, sampleDistance ?? 1f);

        // CRT. Enabling releases the override so the designer's intensity comes back.
        SetFloat(crt.blurOffset, !(pp && GetToggle(GraphicsToggle.CRTBlur)), 0f);
        SetFloat(crt.scanlineIntensity, !(pp && GetToggle(GraphicsToggle.CRTScanlines)), 0f);
        SetFloat(crt.vignetteIntensity, !(pp && GetToggle(GraphicsToggle.CRTVignette)), 0f);
        if (!pp)
            SetBool(crt.usePhosphor, true, false);
        else
            SetBool(crt.usePhosphor, crtPhosphorOn.HasValue, crtPhosphorOn ?? false);

        // Dash layers. Not part of Disable All.
        SetBool(dash.enableEthereal, dashEtherealOn.HasValue, dashEtherealOn ?? true);
        SetBool(dash.enableBlur, dashBlurOn.HasValue, dashBlurOn ?? true);
        SetBool(dash.enableWarp, dashDistortionOn.HasValue, dashDistortionOn ?? true);
        SetBool(dash.enableLines, dashLinesOn.HasValue, dashLinesOn ?? true);
    }

    private void SetFloat(VolumeParameter<float> parameter, bool overrideOn, float value)
    {
        SetOverrideState(parameter, overrideOn);
        if (overrideOn)
            parameter.value = value;
    }

    private void SetBool(VolumeParameter<bool> parameter, bool overrideOn, bool value)
    {
        SetOverrideState(parameter, overrideOn);
        if (overrideOn)
            parameter.value = value;
    }

    private void SetOverrideState(VolumeParameter parameter, bool overrideOn)
    {
        parameter.overrideState = overrideOn;
    }

    // ---- Designer values ----

    // EDIT (ui-render-target): Summary: Evaluates the world volumes into a private stack and reads the designer's values from it.
    // Global volumes only (no trigger), and the override layer is excluded, so our own overrides never show up here.
    private void RefreshDesignerValues()
    {
        if (overrideProfile == null || VolumeManager.instance == null || !VolumeManager.instance.isInitialized) return;

        // Once per frame is enough, rows all refresh together.
        if (designerRefreshFrame == Time.frameCount) return;
        designerRefreshFrame = Time.frameCount;

        if (designerStack == null)
            designerStack = VolumeManager.instance.CreateStack();

        VolumeManager.instance.Update(designerStack, null, designerVolumeMask);
        VolumeStack stack = designerStack;

        Bloom stackBloom = stack.GetComponent<Bloom>();
        if (stackBloom != null)
            designBloomIntensity = stackBloom.intensity.value;

        ChromaticAberration stackChromatic = stack.GetComponent<ChromaticAberration>();
        if (stackChromatic != null)
            designChromaticIntensity = stackChromatic.intensity.value;

        DownsampleVolume stackDownsample = stack.GetComponent<DownsampleVolume>();
        if (stackDownsample != null)
            designPixelSize = stackDownsample.pixelSize.value;

        QuantisedDitherVolume stackQuantise = stack.GetComponent<QuantisedDitherVolume>();
        if (stackQuantise != null)
        {
            designColourSteps = stackQuantise.redSteps.value;
            designDitherStrength = stackQuantise.ditherStrength.value;
            designBayerSize = stackQuantise.bayerSize.value;
            designUsePS1 = stackQuantise.usePS1Matrix.value;
        }

        SharpenVolume stackSharpen = stack.GetComponent<SharpenVolume>();
        if (stackSharpen != null)
        {
            designSharpness = stackSharpen.sharpness.value;
            designSampleDistance = stackSharpen.sampleDistance.value;
        }

        CRTVolume stackCrt = stack.GetComponent<CRTVolume>();
        if (stackCrt != null)
        {
            designBlurOffset = stackCrt.blurOffset.value;
            designScanlineIntensity = stackCrt.scanlineIntensity.value;
            designCrtVignetteIntensity = stackCrt.vignetteIntensity.value;
            designUsePhosphor = stackCrt.usePhosphor.value;
        }

        DashEffectsVolumeComponent stackDash = stack.GetComponent<DashEffectsVolumeComponent>();
        if (stackDash != null)
        {
            designDashEthereal = stackDash.enableEthereal.value;
            designDashBlur = stackDash.enableBlur.value;
            designDashWarp = stackDash.enableWarp.value;
            designDashLines = stackDash.enableLines.value;
        }
    }

    private static int NearestColourStepIndex(float steps)
    {
        int best = 0;
        float bestDiff = float.MaxValue;
        for (int i = 0; i < colourStepOptions.Length; i++)
        {
            float diff = Mathf.Abs(colourStepOptions[i] - steps);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }
        return best;
    }

    private static BayerMatrixSize ToBayerSize(DitherPattern pattern)
    {
        switch (pattern)
        {
            case DitherPattern.TwoByTwo: return BayerMatrixSize.TwoByTwo;
            case DitherPattern.FourByFour: return BayerMatrixSize.FourByFour;
            case DitherPattern.EightByEight: return BayerMatrixSize.EightByEight;
            default: return BayerMatrixSize.SixteenBySixteen;
        }
    }
}
