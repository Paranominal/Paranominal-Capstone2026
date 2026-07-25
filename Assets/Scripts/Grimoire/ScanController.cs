using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Handles the scan and collect mechanics. Hold the scan button to scan objects/enemies,
// press collect to pick up collectable items. Renamed from ALTALTScan.
// Scan registers discoveries in DiscoveryLog (items) or Bestiary (enemies, future).
// Collect adds items to Inventory and bridges to the Grimoire for UI.
public class ScanController : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    InputAction collectAction;
    private ALTGrimoire grimoire;
    private ALTScannableObject currentTarget;

    public ScanReticle reticle;

    private bool inScanMode = false;
    private float scanProgress = 0f;
    public float scanRange = 20f;

    // EDIT (inventory system): references to the new inventory and discovery systems.
    private Inventory inventory;
    private DiscoveryLog discoveryLog;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO itemPickupSound;
    [SerializeField] private SoundDataSO scanSound;
    [SerializeField] private AudioSource audioSource;

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

        if (reticle == null)
            reticle = FindAnyObjectByType<ScanReticle>();

        // EDIT (inventory system): find the new systems.
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }
        if (discoveryLog == null)
        {
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        }
    }

    void Update()
    {
        if (grimoire.grimoireActive)
        {
            ClearHover();
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
            ALTScannableObject target = hit.collider.GetComponent<ALTScannableObject>();

            // Collection runs regardless of scan mode.
            if (collectAction.WasReleasedThisFrame() && target != null && target.collectable)
            {
                CollectItem(target);

                if (target == currentTarget)
                {
                    currentTarget = null;
                    scanProgress = 0f;
                    reticle.SetProgress(0f);
                }
                Destroy(target.gameObject);
                UpdateScanSound(wantScanSoundThisFrame);
                return;
            }

            // Show white outline on whatever we're looking at; hide the previous target.
            if (target != currentTarget)
            {
                if (currentTarget != null)
                {
                    currentTarget.SetOutlineVisible(false);
                }
                scanProgress = 0f;
                currentTarget = target;
                if (currentTarget != null)
                {
                    currentTarget.SetOutlineVisible(true);
                    currentTarget.SetOutlineColor(Color.white);

                    if (inScanMode && grimoire.CompareEntry(currentTarget.entry))
                    {
                        grimoire.SelectEntry(grimoire.GetEntryID(currentTarget.entry.entryName));
                    }
                }
            }

            if (inScanMode && currentTarget != null)
            {
                bool alreadyScanned = grimoire.CompareEntry(currentTarget.entry);
                EnemyStagger scannedEnemy = hit.collider.GetComponentInParent<EnemyStagger>();

                if (!alreadyScanned || scannedEnemy != null)
                {
                    scanProgress += Time.deltaTime / currentTarget.scanDuration;
                    scanProgress = Mathf.Clamp01(scanProgress);

                    currentTarget.SetOutlineColor(new Color(0.0f, 0.941f, 0.459f));

                    wantScanSoundThisFrame = true;

                    if (scanProgress >= 1f)
                    {
                        if (!alreadyScanned)
                        {
                            CompleteScan(currentTarget);
                        }

                        if (scannedEnemy != null)
                        {
                            scannedEnemy.OnEnemyScanned();
                        }

                        currentTarget.SetOutlineColor(Color.white);
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

    // Summary: Registers a completed scan in the Grimoire and DiscoveryLog.
    private void CompleteScan(ALTScannableObject target)
    {
        grimoire.AddEntry(target.entry);

        // EDIT (inventory system): also register in the new discovery system.
        if (target.itemDefinition != null && discoveryLog != null)
        {
            Texture2D snapshot = grimoire.snapshotHandler != null ? grimoire.snapshotHandler.TakeSnapshot() : null;
            discoveryLog.Add(target.itemDefinition, snapshot);
        }
    }

    // Summary: Collects an item into Inventory and registers in Grimoire + DiscoveryLog.
    private void CollectItem(ALTScannableObject target)
    {
        // Grimoire bridge: add or mark as collected.
        if (!grimoire.CompareEntry(target.entry))
        {
            grimoire.AddEntry(target.entry, true);
        }
        else
        {
            grimoire.CollectEntry(target.entry);
        }

        // EDIT (inventory system): add to inventory and auto-discover.
        if (target.itemDefinition != null)
        {
            if (inventory != null)
                inventory.Add(target.itemDefinition, 1);

            if (discoveryLog != null && !discoveryLog.HasDiscovered(target.itemDefinition))
            {
                Texture2D snapshot = grimoire.snapshotHandler != null ? grimoire.snapshotHandler.TakeSnapshot() : null;
                discoveryLog.Add(target.itemDefinition, snapshot);
            }
        }

        Debug.Log("Collected " + target.gameObject);
        AudioManager.PlaySound(itemPickupSound);
    }

    private void SetScanMode(bool active)
    {
        inScanMode = active;

        if (!active)
        {
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
