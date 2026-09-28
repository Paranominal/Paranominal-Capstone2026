using UnityEngine;
using UnityEngine.Rendering;

// Summary: Drives the Impact Frame post-process. Played through Play() by CameraEffectCoordinator (or anything else).
// Holds the full composite (two-tone scene, lines, jitter) like the reference Shader Graph,
// re-rolling the pattern at a set rate the same way the graph's stepped time does.
// Optional flash before the hold and lines-only tail after it. Calling Play() mid-sequence restarts it
// at the new position. Frames are counted in rendered frames, and everything holds while paused.
public class ImpactFrameController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Volume containing the ImpactFrameVolumeComponent. If null, searches the scene.")]
    [SerializeField] private Volume impactFrameVolume = null;
    [Tooltip("Camera used to project the hit position to the screen. If null, Camera.main is used.")]
    [SerializeField] private Camera playerCamera = null;
    [Tooltip("Scene PauseManager. If null, searches the scene.")]
    [SerializeField] private PauseManager pauseManager = null;

    [Header("Timing (frames)")]
    [Tooltip("Solid colour fill before the hold. 0 = off.")]
    [SerializeField, Range(0, 3)] private int flashFrames = 0;
    [Tooltip("How long the full effect is held.")]
    [SerializeField, Min(1)] private int holdFrames = 18;
    [Tooltip("Lines and jitter over the normal scene after the hold. 0 = off.")]
    [SerializeField, Min(0)] private int tailFrames = 0;

    [Header("Re-roll")]
    [Tooltip("How many times per second the lines and jitter pattern re-rolls. 7 = same as the reference graph.")]
    [SerializeField, Range(1f, 30f)] private float stepsPerSecond = 7f;

    private ImpactFrameVolumeComponent impactFrame = null;

    // sequence state
    private bool sequenceRunning = false;
    private int sequenceFrame = 0;
    private int sequenceStartFrame = -1;

    // re-roll state, mirrors the graph's floor(time * -1 * 7)
    private float baseSeed = 0f;
    private float elapsed = 0f;

    public bool IsPlaying => sequenceRunning;

    private int TotalFrames => flashFrames + holdFrames + tailFrames;
    private bool IsPaused => pauseManager != null && pauseManager.IsPaused;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (pauseManager == null)
            pauseManager = FindAnyObjectByType<PauseManager>();

        ResolveVolume();

        // ensure effect starts off
        SetLayers(false, false, false, false);
    }

    private void OnDisable()
    {
        // don't leave the effect stuck on if disabled mid-sequence
        EndSequence();
    }

    // LateUpdate so a Play() from gameplay code always lands before the tick
    private void LateUpdate()
    {
        if (!sequenceRunning)
            return;

        // frame count and re-roll timer hold while paused, and the layers are cut so they don't sit behind the Grimoire
        if (IsPaused)
        {
            SetLayers(false, false, false, false);
            return;
        }

        // frame 0 is applied on the trigger frame and still needs to render
        if (Time.frameCount == sequenceStartFrame)
            return;

        sequenceFrame++;
        elapsed += Time.unscaledDeltaTime;

        if (sequenceFrame >= TotalFrames)
            EndSequence();
        else
            ApplyFrame();
    }

    // EDIT (shot-feedback): public entry point, replaces the SpecialDestroyed and HitstopComplete subscriptions
    public void Play(Vector3 worldPosition)
    {
        if (impactFrame == null)
            return;

        impactFrame.focalPoint.Override(GetFocalPoint(worldPosition));

        // whole number start point so each step lands on the same noise cells the graph's integer time does
        baseSeed = Random.Range(100, 1000);
        elapsed = 0f;

        // restarts cleanly if a previous sequence is still going
        sequenceFrame = 0;
        sequenceStartFrame = Time.frameCount;
        sequenceRunning = true;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        // count the seed down one per step, like the graph's floor(time * -1 * 7)
        impactFrame.seed.Override(baseSeed - Mathf.Floor(elapsed * stepsPerSecond));

        if (sequenceFrame < flashFrames)
            SetLayers(true, false, false, false);
        else if (sequenceFrame < flashFrames + holdFrames)
            SetLayers(false, true, true, true);
        else
            SetLayers(false, false, true, true);
    }

    private void EndSequence()
    {
        sequenceRunning = false;
        SetLayers(false, false, false, false);
    }

    private Vector2 GetFocalPoint(Vector3 worldPosition)
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return new Vector2(0.5f, 0.5f);

        Vector3 viewport = playerCamera.WorldToViewportPoint(worldPosition);

        // behind the camera, fall back to screen centre
        if (viewport.z <= 0f)
            return new Vector2(0.5f, 0.5f);

        return new Vector2(viewport.x, viewport.y);
    }

    private void SetLayers(bool flashOn, bool sceneOn, bool linesOn, bool jitterOn)
    {
        if (impactFrame == null)
            return;

        impactFrame.flashActive.Override(flashOn);
        impactFrame.sceneActive.Override(sceneOn);
        impactFrame.linesActive.Override(linesOn);
        impactFrame.jitterActive.Override(jitterOn);
    }

    // finds the Volume whose profile actually holds the impact frame, then caches the component
    private void ResolveVolume()
    {
        if (impactFrameVolume == null)
        {
            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (volume.sharedProfile != null && volume.sharedProfile.Has<ImpactFrameVolumeComponent>())
                {
                    impactFrameVolume = volume;
                    break;
                }
            }
        }

        // sharedProfile, not profile, so runtime writes hit the active profile
        if (impactFrameVolume != null && impactFrameVolume.sharedProfile != null)
            impactFrameVolume.sharedProfile.TryGet(out impactFrame);

        if (impactFrame == null)
            Debug.LogWarning("ImpactFrameController: no Volume with an ImpactFrameVolumeComponent found.");
    }
}