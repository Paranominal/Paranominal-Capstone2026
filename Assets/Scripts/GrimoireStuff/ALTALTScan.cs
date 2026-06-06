using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ALTALTScan : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    InputAction collectAction;  // i started implementing collecting as a seperate set of scripts and then realised it was going to either be so wrapped up in this as to be problematic or duplicate so much code it would be incredibly questionable. so. voila.
    private ALTGrimoire grimoire;
    private ALTScannableObject currentTarget;

    public ScanModeVisuals visuals;
    public ScanReticle reticle;

    private bool inScanMode = false;
    private float scanProgress = 0f;
    public float scanRange = 20f;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO itemPickupSound;
    [SerializeField] private SoundDataSO scanSound;
    [SerializeField] private AudioSource audioSource; // add Miriam's AudioSource here (she should have two but either one, it doesn't matter lmao)

    // Tracks whether the looping scan sound is currently playing on the AudioSource.
    private bool scanSoundPlaying;

    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
        collectAction = InputSystem.actions.FindAction("Collect");
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    void Update()
    {
        if (grimoire.grimoireActive) return; // shouldn't be able to happen bc action maps, but that may change eventually

        bool wantScanMode = scanAction.IsPressed();
        if (wantScanMode != inScanMode)
        {
            SetScanMode(wantScanMode);
        }

        // Set to true inside the scan logic when actively scanning a valid target.
        // Checked at the end of Update to decide whether the looping scan sound
        // should be playing this frame.
        bool wantScanSoundThisFrame = false;

        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scannable))
        {
            // Debug.DrawLine(transform.position, hit.point, Color.cyan, 10); // can view this in gizmos mode to help with debugging
            ALTScannableObject target = hit.collider.GetComponent<ALTScannableObject>();
            //Debug.Log("Hit " + tempScan);

            // runs regardless of scan mode
            if (collectAction.WasReleasedThisFrame() && target.collectable)
            {
                // item gets collected here
                if (!grimoire.CompareEntry(target.entry))
                {
                    grimoire.AddEntry(target.entry, true);
                }
                else
                {
                    grimoire.CollectEntry(target.entry);
                }
                Debug.Log("Destroyed " + target.gameObject);
                AudioManager.PlaySound(itemPickupSound);

                if (target == currentTarget)
                {
                    currentTarget = null;
                    scanProgress = 0f;
                    reticle.SetProgress(0f);
                }
                Destroy(target.gameObject);
                UpdateScanSound(wantScanSoundThisFrame);
                return; // nothing left to scan this frame
            }

            if (inScanMode)
            {
                if (target != currentTarget)
                {
                    if (currentTarget != null)
                    {
                        visuals.ApplyOutlineColor(currentTarget); // reset previous target's colour if stopped scanning
                    }
                    scanProgress = 0f;
                    currentTarget = target;
                    if (grimoire.CompareEntry(currentTarget.entry)) // opening the previous one if already scanned
                    {
                        grimoire.SelectEntry(grimoire.GetEntryID(currentTarget.entry.entryName));
                    }
                }

                bool alreadyScanned = grimoire.CompareEntry(currentTarget.entry);
                EnemyStagger scannedEnemy = hit.collider.GetComponentInParent<EnemyStagger>();

                if (!alreadyScanned || scannedEnemy != null)
                {
                    scanProgress += Time.deltaTime / currentTarget.scanDuration;
                    scanProgress = Mathf.Clamp01(scanProgress);

                    float pulse = (Mathf.Sin(Time.time * Mathf.Lerp(3f, 8f, scanProgress)) + 1f) / 2f; // makes the fancy pulsing
                    Color scanColor = Color.Lerp(Color.white, Color.green, pulse * scanProgress);
                    currentTarget.SetOutlineColor(scanColor);

                    // Mark that we want the looping scan sound this frame.
                    wantScanSoundThisFrame = true;

                    if (scanProgress >= 1f)
                    {
                        if (!alreadyScanned)
                        {
                            grimoire.AddEntry(currentTarget.entry);
                        }

                        if (scannedEnemy != null)
                        {
                            scannedEnemy.OnEnemyScanned();
                        }

                        visuals.ApplyOutlineColor(currentTarget);
                        scanProgress = 0f;
                    }
                }

                reticle.SetProgress((alreadyScanned && scannedEnemy == null) ? 0f : scanProgress);
            }
        }
        else
        {
            if (currentTarget != null)
            {
                visuals.ApplyOutlineColor(currentTarget); // reset colour when losing target
                currentTarget = null;
            }
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }

        UpdateScanSound(wantScanSoundThisFrame);
    }

    private void SetScanMode(bool active)
    {
        inScanMode = active;
        visuals.SetScanMode(active);

        if (!active)
        {
            if (currentTarget != null)
            {
                visuals.ApplyOutlineColor(currentTarget);
                currentTarget = null;
            }
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }
    }

    // Summary: Starts or stops the looping scan sound based on whether we want it
    // playing this frame. Only triggers Play/Stop on state transitions, so no
    // per-frame restarts.
    private void UpdateScanSound(bool wantPlaying)
    {
        if (wantPlaying && !scanSoundPlaying)
        {
            if (scanSound != null && audioSource != null)
            {
                AudioManager.PlaySound(scanSound, audioSource);
                scanSoundPlaying = true;
            }
        }
        else if (!wantPlaying && scanSoundPlaying)
        {
            if (audioSource != null) audioSource.Stop();
            scanSoundPlaying = false;
        }
    }
}