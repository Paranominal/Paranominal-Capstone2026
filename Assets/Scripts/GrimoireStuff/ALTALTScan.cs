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
        if (grimoire.grimoireActive)
        {
            ClearHover(); // hide any active hover outline when grimoire opens
            return;
        }

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

            // show white outline on whatever we're looking at; hide the previous target
            if (target != currentTarget)
            {
                if (currentTarget != null)
                {
                    currentTarget.SetOutlineVisible(false);
                }
                scanProgress = 0f;
                currentTarget = target;
                currentTarget.SetOutlineVisible(true);
                currentTarget.SetOutlineColor(Color.white);

                if (inScanMode && grimoire.CompareEntry(currentTarget.entry)) // open existing entry when hovering in scan mode
                {
                    grimoire.SelectEntry(grimoire.GetEntryID(currentTarget.entry.entryName));
                }
            }

            if (inScanMode)
            {
                bool alreadyScanned = grimoire.CompareEntry(currentTarget.entry);
                EnemyStagger scannedEnemy = hit.collider.GetComponentInParent<EnemyStagger>();

                if (!alreadyScanned || scannedEnemy != null)
                {
                    scanProgress += Time.deltaTime / currentTarget.scanDuration;
                    scanProgress = Mathf.Clamp01(scanProgress);

                    currentTarget.SetOutlineColor(new Color(0.0f, 0.941f, 0.459f));

                    // Mark that we want the looping scan sound this frame.
                    wantScanSoundThisFrame = true;

                    if (scanProgress >= 1f)
                    {
                        if (!alreadyScanned)
                        {
                            grimoire.AddEntry(currentTarget.entry);
                        }

                        // if (scannedEnemy != null)
                        // {
                        //     scannedEnemy.OnEnemyScanned();
                        // }

                        currentTarget.SetOutlineColor(Color.white); // scan complete, back to plain hover white
                        scanProgress = 0f;
                    }
                }

                reticle.SetProgress((alreadyScanned && scannedEnemy == null) ? 0f : scanProgress);
            }
        }
        else
        {
            ClearHover();
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }

        UpdateScanSound(wantScanSoundThisFrame);
    }

    private void SetScanMode(bool active)
    {
        inScanMode = active;

        if (!active)
        {
            // keep hover outline visible if we're still looking at something,
            // just reset scan progress and revert to plain white
            if (currentTarget != null)
            {
                currentTarget.SetOutlineColor(Color.white);
            }
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }
    }

    private void ClearHover()
    {
        if (currentTarget != null)
        {
            currentTarget.SetOutlineVisible(false);
            currentTarget = null;
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