using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [HideInInspector] public WeakPointManager manager;
    public WeakPointType weakPointType;
    public bool IsTough => isTough;
    public bool IsWarded => isWarded;
    public int RemainingShotsToDestroy => remainingShots;

    [Header("Extra Weakpoint Behaviors")]
    [SerializeField] private bool isWarded;
    [SerializeField] private bool isTough;
    [SerializeField, Min(1)] private int shotsToDestroy = 2;

    [Header("Overlay Visuals")]
    [SerializeField] private SpriteRenderer wardedOverlayRenderer;

    // visuals for each weakpoint type
    [SerializeField] private GameObject ironElement;
    [SerializeField] private GameObject silverElement;

    // Cached runtime references so we avoid repeatedly looking up components
    private GameObject currentElement;
    private SpriteRenderer[] currentRenderers;
    private SpriteRenderer[] allRenderers;
    private SphereCollider weakPointCollider;

    // isShown tracks whether this weakpoint is the currently active target in the weakpoint sequence
    // currentAlpha is smoothed over time to avoid hard pop-in transitions
    private bool isShown;
    private float currentAlpha;
    private bool isUnlocked;
    private int remainingShots;

    private void Awake()
    {
        // cache expensive lookups once at startup for performance and cleaner updating
        weakPointCollider = GetComponent<SphereCollider>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        // Decide which visual branch this weakpoint should use based on its type
        if (weakPointType == WeakPointType.Iron) currentElement = ironElement;
        else if (weakPointType == WeakPointType.Silver) currentElement = silverElement;
        else Debug.Log(gameObject + " is broken!! : weakpoint type is somehow neither iron nor silver!");

        // cache only the active branch's renderers so alpha updates affect the correct visuals
        if (currentElement != null)
            currentRenderers = currentElement.GetComponentsInChildren<SpriteRenderer>(true);

        // ensure weakpoints start hidden until the manager explicitly shows the current target
        Hide();
    }

    private void LateUpdate()
    {
        // skips everything for weakpoints that are not active or have no valid renderers
        if (!isShown || currentRenderers == null || currentRenderers.Length == 0)
            return;

        // Prefer the dedicated weakpoint camera profile; fallback to main camera for robustness
        Camera cam = WeakPointCamera.ActiveCamera != null ? WeakPointCamera.ActiveCamera : Camera.main;

        // exit if there is no camera to check against, though this shouldn't happen since the manager ensures a camera exists before showing weakpoints
        if (cam == null)
            return;

        // Viewport-space check ensures weakpoints only appear when actually on screen
        // x/y in [0..1] = inside camera bounds, z > 0 = in front of camera.
        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        bool inFront = viewport.z > 0f;
        const float viewportPadding = 0.08f;
        bool inViewport =
            viewport.x >= -viewportPadding && viewport.x <= 1f + viewportPadding &&
            viewport.y >= -viewportPadding && viewport.y <= 1f + viewportPadding;

        // Distance check controls long-range visibility and fade behavior
        float distance = Vector3.Distance(cam.transform.position, transform.position);
        bool inRange = distance <= WeakPointCamera.MaxRenderDistance;

        // Default hidden unless all visibility conditions pass
        float targetAlpha = 0f;
        if (inFront && inViewport && inRange)
        {
            // Map distance to alpha:
            // - at MaxRenderDistance => 0
            // - at FullAlphaDistance (or closer) => 1
            targetAlpha = Mathf.InverseLerp(
                WeakPointCamera.MaxRenderDistance,
                WeakPointCamera.FullAlphaDistance,
                distance);
        }

        // Smoothly approach target alpha each frame to remove abrupt pops.
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            WeakPointCamera.FadeSpeed * Time.deltaTime);

        // Apply final opacity to active weakpoint sprites
        ApplyAlpha(currentAlpha);
    }

    public void Show(WeakPointType _)
    {
        // Mark as currently active in sequence and re-enable hit detection.
        isShown = true;
        isUnlocked = !isWarded;
        remainingShots = isTough ? Mathf.Max(1, shotsToDestroy) : 1;

        if (weakPointCollider != null)
            weakPointCollider.enabled = true;

        // reset all child visuals first, then only enable the selected branch
        foreach (SpriteRenderer renderer in allRenderers)
            renderer.enabled = false;

        if (currentRenderers == null)
            return;

        foreach (SpriteRenderer renderer in currentRenderers)
            renderer.enabled = true;

        // start from transparent so entry into view/range fades in smoothly
        currentAlpha = 0f;
        ApplyAlpha(currentAlpha);
    }

    public void Hide()
    {
        // Deactivate both visuals and collider so hidden weakpoints can't be hit
        isShown = false;

        if (weakPointCollider != null)
            weakPointCollider.enabled = false;

        foreach (SpriteRenderer renderer in allRenderers)
            renderer.enabled = false;

        if (wardedOverlayRenderer != null)
            wardedOverlayRenderer.enabled = false;

        // Keep state reset so the next show starts from a clean fade
        currentAlpha = 0f;
    }

    public void OnHit(WeakPointType type)
    {
        // Ignore mismatched bullet types to enforce iron/silver behavior
        if (type != weakPointType) return;

        // Warded weakpoints cannot be destroyed until unlocked externally
        if (isWarded && !isUnlocked) return;

        // Tough weakpoints require multiple successful hits
        remainingShots -= 1;
        if (remainingShots > 0) return;

        // Correct hit: hide this point and advance sequence to the next one
        Hide();
        manager.NextWeakPoint();
    }

    public void UnlockWeakPoint()
    {
        isUnlocked = true;
        UpdateWardedOverlay(currentAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        // push alpha to each renderer and disable close to zero sprites
        foreach (SpriteRenderer renderer in currentRenderers)
        {
            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
            renderer.enabled = alpha > 0.001f;
        }

        UpdateWardedOverlay(alpha);
    }

    private void UpdateWardedOverlay(float alpha)
    {
        if (wardedOverlayRenderer == null)
            return;

        bool showOverlay = isShown && isWarded && !isUnlocked && alpha > 0.001f;
        wardedOverlayRenderer.enabled = showOverlay;

        if (!showOverlay)
            return;

        Color c = wardedOverlayRenderer.color;
        c.a = alpha;
        wardedOverlayRenderer.color = c;
    }
}