// Summary: Centralized resolution management. Controls URP render scale to set the internal
// rendering resolution the player selects, and exposes a global shader float (_ResolutionScale)
// so post-process effects stay visually consistent across resolutions. The display/window
// resolution is left untouched. Lives on GameSystems (persistent).
// EDIT (RenderResolutionManager): Reverted from locked 1080p internal rendering back to player-selected
// internal resolution, with _ResolutionScale driving effect scaling in the post-process shaders.

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderResolutionManager : MonoBehaviour
{
    public static RenderResolutionManager Instance { get; private set; }

    // EDIT (RenderResolutionManager): Renamed from InternalWidth/InternalHeight. 1080p is now the reference
    // resolution all post-process effects are authored around, not a fixed internal resolution.
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    // Fires when the selected render resolution changes. Args: target width, target height.
    public event Action<int, int> OnResolutionChanged;

    // EDIT (RenderResolutionManager): Current resolution scale relative to 1080p (e.g. 2.0 at 4K).
    public float ResolutionScale { get; private set; } = 1f;

    // The internal render resolution the player has selected.
    public Vector2Int SelectedResolution { get; private set; }

    // EDIT (RenderResolutionManager): Width is tracked too so window resizes are caught either way.
    private int cachedScreenWidth;
    private int cachedScreenHeight;
    private UniversalRenderPipelineAsset pipelineAsset;

    // EDIT (RenderResolutionManager): Render scale on the asset before we changed it, restored on destroy
    // so exiting play mode doesn't leave the pipeline asset modified.
    private float originalRenderScale = 1f;

    // Shader property ID for the global resolution scale.
    private static readonly int ResolutionScaleID = Shader.PropertyToID("_ResolutionScale");

    // Target render resolutions shown in the picker.
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
        else
            originalRenderScale = pipelineAsset.renderScale;

        // Default to the current screen resolution.
        SelectedResolution = new Vector2Int(Screen.width, Screen.height);
        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        // EDIT (RenderResolutionManager): Shader global now set in ApplyRenderScale from the selected resolution.
        ApplyRenderScale();
    }

    private void Update()
    {
        // EDIT (RenderResolutionManager): If the display/window resized, recalculate render scale to keep the selected target.
        if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
        {
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;
            ApplyRenderScale();
        }
    }

    // EDIT (RenderResolutionManager): Sets the internal render resolution. No longer calls Screen.SetResolution,
    // so the fullscreen parameter was removed.
    public void SetResolution(int width, int height)
    {
        SelectedResolution = new Vector2Int(width, height);
        ApplyRenderScale();
    }

    // Returns the full list of target render resolutions.
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

    // EDIT (RenderResolutionManager): Sets URP render scale to hit the selected resolution, and updates the shader
    // effect scale relative to the 1080p reference.
    private void ApplyRenderScale()
    {
        if (pipelineAsset == null) return;

        // Render scale relative to the actual screen/window size. Above 1.0 supersamples.
        float renderScale = (float)SelectedResolution.y / Screen.height;
        pipelineAsset.renderScale = Mathf.Clamp(renderScale, 0.1f, 2.0f);

        // Shader effect scale relative to the 1080p reference.
        ResolutionScale = (float)SelectedResolution.y / ReferenceHeight;
        Shader.SetGlobalFloat(ResolutionScaleID, ResolutionScale);

        OnResolutionChanged?.Invoke(SelectedResolution.x, SelectedResolution.y);
    }

    // EDIT (RenderResolutionManager): Restore the pipeline asset's render scale.
    private void OnDestroy()
    {
        if (Instance != this) return;

        if (pipelineAsset != null)
            pipelineAsset.renderScale = originalRenderScale;

        Instance = null;
    }
}
