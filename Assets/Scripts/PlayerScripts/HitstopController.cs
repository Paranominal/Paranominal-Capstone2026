using UnityEngine;

// Summary: Freezes gameplay via Time.timeScale. Lives in SceneEssentials and is driven by CameraEffectCoordinator
// (or anything else that needs a freeze). Play() scales the default duration by a strength multiplier,
// PlayFor() takes an exact length. Calling either mid-freeze restarts the timer. Dropped if the game pauses mid-freeze.
public class HitstopController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Scene PauseManager. If null, searches the scene.")]
    [SerializeField] private PauseManager pauseManager = null;

    // duration settings moved here from the coordinator
    [Header("Settings")]
    [Tooltip("Freeze length at full strength, in real seconds. 0 = off.")]
    [SerializeField, Min(0f)] private float duration = 0.1f;
    [Tooltip("Scaled freezes never drop below this.")]
    [SerializeField, Min(0f)] private float minDuration = 0.04f;

    // other systems check this to know whether gameplay is frozen
    public static bool IsActive { get; private set; }

    private float currentDuration;
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

        // pause owns timeScale now, so drop the freeze without restoring it
        if (IsPaused)
        {
            IsActive = false;
            return;
        }

        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= currentDuration)
            End();
    }

    // default duration scaled by strength (1 = full), clamped to the min
    public void Play(float strength = 1f)
    {
        if (duration <= 0f)
            return;

        PlayFor(Mathf.Max(duration * strength, minDuration));
    }

    // exact freeze length in real seconds, ignores the settings above
    public void PlayFor(float seconds)
    {
        if (seconds <= 0f || IsPaused)
            return;

        // only store the time scale on a fresh freeze, mid-freeze it's already 0
        if (!IsActive)
            timeScaleBefore = Time.timeScale;

        currentDuration = seconds;
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