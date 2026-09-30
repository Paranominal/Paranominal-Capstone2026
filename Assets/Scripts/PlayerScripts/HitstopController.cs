using UnityEngine;

// Summary: Freezes gameplay via Time.timeScale for a set duration. Lives in SceneEssentials and is driven
// through Play() by CameraEffectCoordinator (or anything else that needs a freeze). Calling Play() mid-freeze
// restarts the timer with the new duration. Dropped if the game pauses mid-freeze.
public class HitstopController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Scene PauseManager. If null, searches the scene.")]
    [SerializeField] private PauseManager pauseManager = null;

    // other systems check this to know whether gameplay is frozen
    public static bool IsActive { get; private set; }

    private float duration;
    private float elapsed;
    private float timeScaleBefore = 1f;

    private bool IsPaused => pauseManager != null && pauseManager.IsPaused;

    private void Awake()
    {
        if (pauseManager == null)
            pauseManager = FindAnyObjectByType<PauseManager>();
    }

    private void OnDisable()
    {
        // don't leave timeScale stuck at 0 if disabled mid-freeze
        if (IsActive)
            End();
    }

    private void Update()
    {
        if (!IsActive)
            return;

        // EDIT (shot-feedback): pause owns timeScale now, so drop the freeze without restoring it
        if (IsPaused)
        {
            IsActive = false;
            return;
        }

        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= duration)
            End();
    }

    // EDIT (shot-feedback): public entry point, replaces the SpecialDestroyed subscription
    public void Play(float freezeDuration)
    {
        if (freezeDuration <= 0f || IsPaused)
            return;

        // only store the time scale on a fresh freeze, mid-freeze it's already 0
        if (!IsActive)
            timeScaleBefore = Time.timeScale;

        duration = freezeDuration;
        elapsed = 0f;

        Time.timeScale = 0f;
        IsActive = true;
    }

    private void End()
    {
        Time.timeScale = timeScaleBefore;
        IsActive = false;
    }
}