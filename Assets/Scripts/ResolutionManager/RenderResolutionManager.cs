// Summary: Centralized resolution management. Locks the internal rendering resolution to
// 1080p via URP render scale, while the display/output resolution is controlled separately
// via Screen.SetResolution. Post-processing always sees a 1080p render target.
// Lives on GameSystems (persistent).

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderResolutionManager : MonoBehaviour
{
    public static RenderResolutionManager Instance { get; private set; }

    // Internal render resolution is always 1080p.
    public const int InternalWidth = 1920;
    public const int InternalHeight = 1080;

    // Fires when the display resolution changes. Args: display width, display height.
    public event Action<int, int> OnResolutionChanged;

    // The display resolution the player has selected.
    public Vector2Int SelectedResolution { get; private set; }

    private int cachedScreenHeight;
    private UniversalRenderPipelineAsset pipelineAsset;

    // Shader property ID for the global resolution scale (always 1.0 since internal res is fixed).
    private static readonly int ResolutionScaleID = Shader.PropertyToID("_ResolutionScale");

    // Target display resolutions shown in the picker.
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

        SelectedResolution = new Vector2Int(Screen.width, Screen.height);
        cachedScreenHeight = Screen.height;

        // Internal res is always 1080p, so effect scale is always 1.0.
        Shader.SetGlobalFloat(ResolutionScaleID, 1f);
        ApplyRenderScale();
    }

    private void Update()
    {
        // If the display/window height changed, recalculate render scale to maintain 1080p internal.
        if (Screen.height != cachedScreenHeight)
        {
            cachedScreenHeight = Screen.height;
            ApplyRenderScale();
        }
    }

    // Sets the display resolution. Internal rendering stays at 1080p.
    public void SetResolution(int width, int height, bool fullscreen)
    {
        SelectedResolution = new Vector2Int(width, height);
        Screen.SetResolution(width, height, fullscreen);
    }

    // Returns the full list of target display resolutions.
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

    // Adjusts URP render scale so the internal render target is always 1080p.
    private void ApplyRenderScale()
    {
        if (pipelineAsset == null) return;

        pipelineAsset.renderScale = (float)InternalHeight / Screen.height;
    }
}