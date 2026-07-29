using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Handles the scan mechanic. Hold the scan button to scan objects/enemies.
// EDIT (grimoire-decoupling): no longer references ALTGrimoire directly.
// Suppression uses OnGrimoireToggled event; auto-select replaced with a static
// OnScannableHovered event that the grimoire can subscribe to.
public class ScanController : MonoBehaviour
{
    InputAction scanAction;

    private IScannable currentTarget;

    public ScanReticle reticle;

    private bool inScanMode = false;
    private float scanProgress = 0f;
    public float scanRange = 20f;

    private PhotoSnapshots snapshotHandler;

    // EDIT (weapon system): two suspension sources tracked separately.
    private bool suspendedByGrimoire;
    private bool suspendedByWeapon;
    private bool suspended => suspendedByGrimoire || suspendedByWeapon;

    // EDIT (grimoire-decoupling): fired when hovering an already-discovered WorldItem in scan mode.
    // The grimoire (or any future UI) subscribes to this instead of being called directly.
    public static event System.Action<ItemDefinition> OnScannableHovered;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO scanSound;
    [SerializeField] private AudioSource audioSource;

    private bool scanSoundPlaying;

    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");

        if (snapshotHandler == null)
            snapshotHandler = FindAnyObjectByType<PhotoSnapshots>();
        if (reticle == null)
            reticle = FindAnyObjectByType<ScanReticle>();

        ALTGrimoire grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (grimoire != null)
            grimoire.OnGrimoireToggled += OnGrimoireToggled;

        // EDIT (weapon system): suspend scanning when a weapon is equipped.
        WeaponManager.OnWeaponModeChanged += OnWeaponModeChanged;
    }

    void OnDestroy()
    {
        ALTGrimoire grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (grimoire != null)
            grimoire.OnGrimoireToggled -= OnGrimoireToggled;

        WeaponManager.OnWeaponModeChanged -= OnWeaponModeChanged;
    }

    private void OnGrimoireToggled(bool grimoireOpen)
    {
        // EDIT (weapon system): use named flag instead of shared bool.
        suspendedByGrimoire = grimoireOpen;
        if (suspendedByGrimoire)
            ClearHover();
    }

    // EDIT (weapon system): suspend scanning while a weapon is equipped.
    private void OnWeaponModeChanged(bool weaponEquipped)
    {
        suspendedByWeapon = weaponEquipped;
        if (suspendedByWeapon)
            ClearHover();
    }

    void Update()
    {
        if (suspended) return;

        bool wantScanMode = scanAction.IsPressed();
        if (wantScanMode != inScanMode)
        {
            SetScanMode(wantScanMode);
        }

        bool wantScanSoundThisFrame = false;

        Raycaster raycaster = Raycaster.Instance;
        bool rayHit = raycaster != null && raycaster.HasHit && raycaster.Hit.distance <= scanRange;

        if (rayHit)
        {
            IScannable target = raycaster.Hit.collider.GetComponentInParent<IScannable>();

            // Update outline on target change.
            if (target != currentTarget)
            {
                if (currentTarget != null)
                    currentTarget.SetOutlineVisible(false);

                scanProgress = 0f;
                currentTarget = target;

                if (currentTarget != null)
                {
                    currentTarget.SetOutlineVisible(true);
                    currentTarget.SetOutlineColor(Color.white);

                    // EDIT (grimoire-decoupling): fire event instead of calling grimoire directly.
                    if (inScanMode && currentTarget is WorldItem hoveredItem
                        && hoveredItem.itemDefinition != null && hoveredItem.IsDiscovered)
                    {
                        OnScannableHovered?.Invoke(hoveredItem.itemDefinition);
                    }
                }
            }

            // Drive scan progress.
            if (inScanMode && currentTarget != null)
            {
                bool canScan = !currentTarget.IsDiscovered || currentTarget.IsRescannable;

                if (canScan)
                {
                    scanProgress += Time.deltaTime / currentTarget.ScanDuration;
                    scanProgress = Mathf.Clamp01(scanProgress);

                    currentTarget.SetOutlineColor(new Color(0.0f, 0.941f, 0.459f));

                    wantScanSoundThisFrame = true;

                    if (scanProgress >= 1f)
                    {
                        CompleteScan(currentTarget);
                        currentTarget.SetOutlineColor(Color.white);
                        scanProgress = 0f;
                    }
                }

                reticle.SetProgress(canScan ? scanProgress : 0f);
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

    private void CompleteScan(IScannable target)
    {
        Texture2D snapshot = snapshotHandler != null ? snapshotHandler.TakeSnapshot() : null;
        target.OnScanComplete(snapshot);
    }

    private void SetScanMode(bool active)
    {
        inScanMode = active;

        if (!active)
        {
            if (currentTarget != null)
                currentTarget.SetOutlineColor(Color.white);

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
