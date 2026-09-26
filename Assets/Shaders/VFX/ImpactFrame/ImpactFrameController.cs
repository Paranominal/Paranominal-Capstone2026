using UnityEngine;
using UnityEngine.Rendering;

// Summary: Drives the Impact Frame post-process when a Special weakpoint is destroyed.
// Each layer (flash, two-tone scene, lines, jitter) hard cuts on and off on its own frame window,
// centred on the weakpoint's screen position. Optional hitstop, which the sequence can either
// play during or wait for. Everything counts rendered frames and holds while paused.
public class ImpactFrameController : MonoBehaviour
{
    // Summary: A window of frames a layer is shown for, counted from the start of the sequence.
    [System.Serializable]
    public struct FrameWindow
    {
        [Tooltip("Frame the layer turns on. 0 = the first frame of the sequence.")]
        [Min(0)] public int startFrame;
        [Tooltip("How many frames the layer stays on. 0 = layer disabled.")]
        [Min(0)] public int frameCount;

        public FrameWindow(int startFrame, int frameCount)
        {
            this.startFrame = startFrame;
            this.frameCount = frameCount;
        }

        public int EndFrame => frameCount > 0 ? startFrame + frameCount : 0;
        public bool IsOn(int frame) => frameCount > 0 && frame >= startFrame && frame < startFrame + frameCount;
    }

    [Header("References")]
    [Tooltip("Volume containing the ImpactFrameVolumeComponent. If null, searches the scene.")]
    [SerializeField] private Volume impactFrameVolume = null;
    [Tooltip("Camera used to project the weakpoint to the screen. If null, Camera.main is used.")]
    [SerializeField] private Camera playerCamera = null;
    [Tooltip("Scene PauseManager. If null, searches the scene.")]
    [SerializeField] private PauseManager pauseManager = null;

    [Header("Layer Timing (frames)")]
    [Tooltip("Solid fill of the flash colour. Overrides every other layer while on.")]
    [SerializeField] private FrameWindow flash = new FrameWindow(0, 1);
    [Tooltip("Two-tone thresholded scene.")]
    [SerializeField] private FrameWindow scene = new FrameWindow(1, 3);
    [Tooltip("Radial speed lines. Draw solid light when the two-tone scene is off.")]
    [SerializeField] private FrameWindow lines = new FrameWindow(2, 4);
    [Tooltip("UV jitter pushing segments of the scene outward.")]
    [SerializeField] private FrameWindow jitter = new FrameWindow(1, 1);

    [Header("Hitstop")]
    [Tooltip("Freeze time when the impact frame triggers.")]
    [SerializeField] private bool useHitstop = true;
    [Tooltip("How many frames time stays frozen.")]
    [SerializeField, Range(6, 30)] private int hitstopFrames = 12;
    [Tooltip("Hold on the normal scene during the freeze, then play the sequence as time resumes.")]
    [SerializeField] private bool playAfterHitstop = false;

    private ImpactFrameVolumeComponent impactFrame = null;

    // sequence state
    private bool sequenceRunning = false;
    private bool sequencePending = false;
    private int sequenceFrame = 0;
    private int sequenceLength = 0;
    private int sequenceStartFrame = -1;

    // hitstop state
    private bool hitstopActive = false;
    private int hitstopFramesLeft = 0;

    // stops both counters ticking twice in one rendered frame, or on the trigger frame
    private int lastTickFrame = -1;

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

    private void OnEnable()
    {
        WeakPoint.SpecialDestroyed += OnSpecialDestroyed;
    }

    private void OnDisable()
    {
        WeakPoint.SpecialDestroyed -= OnSpecialDestroyed;

        // don't leave the effect or the freeze stuck on if disabled mid-trigger
        EndSequence();
        sequencePending = false;
        EndHitstop();
    }

    // LateUpdate so the trigger (fired during gameplay Update) always lands before the tick
    private void LateUpdate()
    {
        if (!sequenceRunning && !hitstopActive)
            return;

        // counters hold while paused, and the layers are cut so they don't sit behind the Grimoire
        if (IsPaused)
        {
            SetLayers(false, false, false, false);
            return;
        }

        if (Time.frameCount == lastTickFrame)
            return;
        lastTickFrame = Time.frameCount;

        if (hitstopActive)
        {
            // ResumeGame resets timeScale to 1, so reapply the freeze until the hitstop is done
            Time.timeScale = 0f;

            hitstopFramesLeft--;
            if (hitstopFramesLeft <= 0)
            {
                EndHitstop();

                if (sequencePending)
                    StartSequence();
            }
        }

        // advance, but not on the frame the sequence started (frame 0 still needs to render)
        if (sequenceRunning && sequenceStartFrame != Time.frameCount)
        {
            sequenceFrame++;

            if (sequenceFrame >= sequenceLength)
                EndSequence();
            else
                ApplyFrame(sequenceFrame);
        }
    }

    private void OnSpecialDestroyed(Vector3 worldPosition)
    {
        lastTickFrame = Time.frameCount;

        if (impactFrame != null)
        {
            impactFrame.focalPoint.Override(GetFocalPoint(worldPosition));
            impactFrame.seed.Override(Random.Range(0f, 100f));
        }

        bool startHitstop = useHitstop && !IsPaused;

        if (startHitstop)
        {
            hitstopFramesLeft = hitstopFrames;
            hitstopActive = true;
            Time.timeScale = 0f;
        }

        // restart cleanly if a previous sequence is still going
        EndSequence();

        if (startHitstop && playAfterHitstop)
            sequencePending = true;
        else
            StartSequence();
    }

    private void StartSequence()
    {
        sequencePending = false;

        sequenceLength = Mathf.Max(flash.EndFrame, scene.EndFrame, lines.EndFrame, jitter.EndFrame);
        if (sequenceLength <= 0 || impactFrame == null)
            return;

        sequenceFrame = 0;
        sequenceStartFrame = Time.frameCount;
        sequenceRunning = true;
        ApplyFrame(0);
    }

    private void EndSequence()
    {
        sequenceRunning = false;
        SetLayers(false, false, false, false);
    }

    private void ApplyFrame(int frame)
    {
        SetLayers(flash.IsOn(frame), scene.IsOn(frame), lines.IsOn(frame), jitter.IsOn(frame));
    }

    private void EndHitstop()
    {
        if (!hitstopActive)
            return;

        hitstopActive = false;

        // never override a pause
        if (!IsPaused)
            Time.timeScale = 1f;
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
