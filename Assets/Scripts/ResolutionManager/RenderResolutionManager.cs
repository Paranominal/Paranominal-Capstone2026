// Summary: Centralized resolution management. 
// Controls URP render scale to set the internal rendering resolution, and exposes a global shader float (_ResolutionScale) 
// so post-process effects stay visually consistent across resolutions.

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderResolutionManager : MonoBehaviour
{
    public static RenderResolutionManager Instance { get; private set; }

    // Reference resolution that all shaders and UI are designed around.
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    // Fires when the selected render resolution changes. Args: target width, target height.
    public event Action<int, int> OnResolutionChanged;

    // Current resolution scale relative to 1080p (e.g. 2.0 at 4K).
    public float ResolutionScale { get; private set; } = 1f;

    // The render resolution the player has selected.
    public Vector2Int SelectedResolution { get; private set; }

    // Cached screen dimensions to detect display/window size changes.
    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private UniversalRenderPipelineAsset pipelineAsset;

    private static readonly int ResolutionScaleID = Shader.PropertyToID("_ResolutionScale");

    // Target resolutions shown in the picker.
    private static readonly Vector2Int[] targetResolutions = new Vector2Int[]
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipelineAsset == null)
            Debug.LogError("RenderResolutionManager: No UniversalRenderPipelineAsset found.");

        // Default to the current screen resolution.
        SelectedResolution = new Vector2Int(Screen.width, Screen.height);
        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;
        ApplyRenderScale();
    }

    private void Update()
    {
        // If the display/window resized, recalculate render scale to maintain the selected target.
        if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
        {
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;
            ApplyRenderScale();
        }
    }

    // Sets the target render resolution and applies the corresponding URP render scale.
    public void SetResolution(int width, int height)
    {
        SelectedResolution = new Vector2Int(width, height);
        ApplyRenderScale();
    }

    // Returns the full list of target resolutions (all are always shown in the picker).
    public Vector2Int[] GetTargetResolutions()
    {
        return targetResolutions;
    }

    // Checks whether a resolution fits within the player's display.
    public bool IsResolutionSupported(Vector2Int resolution)
    {
        Resolution maxDisplay = Screen.currentResolution;
        return resolution.x <= maxDisplay.width && resolution.y <= maxDisplay.height;
    }

    private void ApplyRenderScale()
    {
        if (pipelineAsset == null) return;

        // Render scale relative to the actual screen/window size.
        float renderScale = (float)SelectedResolution.y / Screen.height;
        pipelineAsset.renderScale = Mathf.Clamp(renderScale, 0.1f, 2.0f);

        // Shader effect scale relative to the 1080p reference.
        ResolutionScale = (float)SelectedResolution.y / ReferenceHeight;
        Shader.SetGlobalFloat(ResolutionScaleID, ResolutionScale);

        OnResolutionChanged?.Invoke(SelectedResolution.x, SelectedResolution.y);
    }
}