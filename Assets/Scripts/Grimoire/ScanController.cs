using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Handles the scan and collect mechanics. Hold the scan button to scan objects/enemies,
// press collect to pick up collectable items.
// EDIT (grimoire migration): all grimoire dual-write calls removed. Scans write to DiscoveryLog only,
// collection writes to Inventory + DiscoveryLog only. The grimoire reads from those systems.
public class ScanController : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    InputAction collectAction;
    private ALTScannableObject currentTarget;

    public ScanReticle reticle;

    private bool inScanMode = false;
    private float scanProgress = 0f;
    public float scanRange = 20f;

    private Inventory inventory;
    private DiscoveryLog discoveryLog;
    private ALTGrimoire grimoire;   // only used for grimoireActive UI state check
    private PhotoSnapshots snapshotHandler;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO itemPickupSound;
    [SerializeField] private SoundDataSO scanSound;
    [SerializeField] private AudioSource audioSource;

    private bool scanSoundPlaying;

    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
        collectAction = InputSystem.actions.FindAction("Collect");

        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        if (grimoire == null)
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (snapshotHandler == null)
            snapshotHandler = FindAnyObjectByType<PhotoSnapshots>();
        if (reticle == null)
            reticle = FindAnyObjectByType<ScanReticle>();
    }

    void Update()
    {
        // Suppress scanning while the grimoire UI is open.
        if (grimoire != null && grimoire.grimoireActive)
        {
            ClearHover();
            return;
        }

        bool wantScanMode = scanAction.IsPressed();
        if (wantScanMode != inScanMode)
        {
            SetScanMode(wantScanMode);
        }

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

                    // EDIT (grimoire migration): auto-select the grimoire page for already-scanned items.
                    if (inScanMode && currentTarget.itemDefinition != null && discoveryLog.HasDiscovered(currentTarget.itemDefinition))
                    {
                        if (grimoire != null)
                            grimoire.SelectByItem(currentTarget.itemDefinition);
                    }
                }
            }

            if (inScanMode && currentTarget != null)
            {
                // EDIT (grimoire migration): "already scanned" now checks DiscoveryLog, not Grimoire.
                bool alreadyScanned = currentTarget.itemDefinition != null && discoveryLog.HasDiscovered(currentTarget.itemDefinition);
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

    // Summary: Registers a completed scan in DiscoveryLog with a snapshot.
    private void CompleteScan(ALTScannableObject target)
    {
        if (target.itemDefinition == null || discoveryLog == null) return;

        Texture2D snapshot = snapshotHandler != null ? snapshotHandler.TakeSnapshot() : null;
        discoveryLog.Add(target.itemDefinition, snapshot);
    }

    // Summary: Collects an item into Inventory and auto-discovers it.
    private void CollectItem(ALTScannableObject target)
    {
        if (target.itemDefinition == null) return;

        if (inventory != null)
            inventory.Add(target.itemDefinition, target.pickupQuantity);

        if (discoveryLog != null && !discoveryLog.HasDiscovered(target.itemDefinition))
        {
            Texture2D snapshot = snapshotHandler != null ? snapshotHandler.TakeSnapshot() : null;
            discoveryLog.Add(target.itemDefinition, snapshot);
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
