using UnityEngine;

public class WeakPointCamera : MonoBehaviour
{
    // Camera reference that drives weakpoint visibility checks.
    [SerializeField] private Camera weakPointCamera;
    // weakpoints farther than this are fully hidden
    [SerializeField] private float maxRenderDistance = 25f;
    // distance where weakpoints should already be fully visible, between maxRenderDistance and this value, weakpoints fade
    [SerializeField] private float fullAlphaDistance = 15f;
    // How quickly points fade in (higher = faster)
    [SerializeField] private float fadeSpeed = 8f;

    // Static values make the active player's weakpoint-camera settings globally accessible
    public static Camera ActiveCamera { get; private set; }
    public static float MaxRenderDistance { get; private set; }
    public static float FullAlphaDistance { get; private set; }
    public static float FadeSpeed { get; private set; }

    private void OnEnable()
    {
        // Refresh global settings when this component becomes active
        Apply();
    }

    private void OnValidate()
    {
        // Also refresh in editor whenever serialized values change
        // so tuning is reflected immediately without entering play mode.
        Apply();
    }

    private void Apply()
    {
        if (weakPointCamera == null)
            weakPointCamera = GetComponent<Camera>();

        // set runtime values for all WeakPoint instances
        ActiveCamera = weakPointCamera;
        // safe minimums to prevent invalid math (division by zero, negative ranges)
        MaxRenderDistance = Mathf.Max(0.01f, maxRenderDistance);
        FullAlphaDistance = Mathf.Clamp(fullAlphaDistance, 0f, MaxRenderDistance);
        FadeSpeed = Mathf.Max(0.01f, fadeSpeed);
    }
}