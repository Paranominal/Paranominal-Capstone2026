// Summary: 
// Centralized resolution management. 
// Tracks the current screen resolution against a 1080p reference, exposes a scale factor as a global shader float (_ResolutionScale),
// and provides a list of target resolutions for the settings UI.

using System;
using UnityEngine;

public class RenderResolutionManager : MonoBehaviour
{
    public static RenderResolutionManager Instance { get; private set; }

    // Reference resolution that all shaders and UI are designed around.
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    // Fires when screen resolution changes. Args: new width, new height.
    public event Action<int, int> OnResolutionChanged;

    // Current resolution scale relative to 1080p (e.g. 2.0 at 4K).
    public float ResolutionScale { get; private set; } = 1f;

    // The resolution the player has selected (may not have applied yet).
    public Vector2Int SelectedResolution { get; private set; }

    // Cached current resolution to detect changes.
    private int cachedWidth;
    private int cachedHeight;

    // Shader property ID for the global resolution scale.
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

        SelectedResolution = new Vector2Int(Screen.width, Screen.height);
        ApplyResolution(Screen.width, Screen.height);
    }

    private void Update()
    {
        // Detect actual resolution changes (Screen.SetResolution is deferred to end-of-frame).
        if (Screen.width != cachedWidth || Screen.height != cachedHeight)
            ApplyResolution(Screen.width, Screen.height);
    }

    // Requests a resolution change. The actual apply happens next frame when Screen catches up.
    public void SetResolution(int width, int height, bool fullscreen)
    {
        SelectedResolution = new Vector2Int(width, height);
        Screen.SetResolution(width, height, fullscreen);
    }

    // Returns the full list of target resolutions (all are always shown).
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

    // Updates cached values, shader global, and fires the event.
    private void ApplyResolution(int width, int height)
    {
        cachedWidth = width;
        cachedHeight = height;

        ResolutionScale = (float)height / ReferenceHeight;
        Shader.SetGlobalFloat(ResolutionScaleID, ResolutionScale);

        OnResolutionChanged?.Invoke(width, height);
    }
}