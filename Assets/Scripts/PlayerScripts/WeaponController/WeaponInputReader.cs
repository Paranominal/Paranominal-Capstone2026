using UnityEngine;
using UnityEngine.InputSystem;

// Michael edit (special-shot): what the player's input resolved to this frame.
public enum ShotIntent { None, Iron, Silver, Special }

public class WeaponInputReader : MonoBehaviour
{
    [Header("Input Actions")]

    [SerializeField] private InputActionReference shootIronAction;
    [SerializeField] private InputActionReference shootSilverAction;
    [SerializeField] private InputActionReference reloadAction;
    // Michael edit (special-shot): input to arm the Special Shot.
    [SerializeField] private InputActionReference specialShotAction;
    [Tooltip("The ammount of time in seconds that the game waits to see if the player wants to charge a special shot")]
    [SerializeField] private float specialShotBuffer;
    // Michael edit (special-shot): read by ShotOrchestrator to delay normal shots while the Special Shot is available.
    public float SpecialShotBuffer => specialShotBuffer;
    private bool canShoot = true;
    public bool CanShoot => canShoot;

    // Michael edit (special-shot): shot input state machine. Idle > Pending (buffer window) > Single or Charging > Idle.
    private enum ShotInputState { Idle, Pending, Single, Charging }
    private ShotInputState shotState = ShotInputState.Idle;
    private WeakPointType pendingType;
    private float pendingTimer;
    private int intentFrame = -1;
    private ShotIntent currentIntent = ShotIntent.None;

    // Michael edit (special-shot): set by ShotOrchestrator. When false there's no buffer, so normal shots fire on press.
    public bool SpecialShotAvailable { get; set; }
    // Michael edit (special-shot): true while both buttons are held with the Special Shot available (for charge VFX).
    public bool IsChargingSpecial => shotState == ShotInputState.Charging && SpecialShotAvailable;

    private float buffer;
    public bool WasIronPressedThisFrame() => shootIronAction != null && shootIronAction.action.WasPerformedThisFrame() && !shootSilverAction.action.IsInProgress();
    public bool WasSilverPressedThisFrame() => shootSilverAction != null && shootSilverAction.action.WasPerformedThisFrame() && !shootIronAction.action.IsInProgress();
    public bool WasReloadPressedThisFrame() => reloadAction != null && reloadAction.action.WasPressedThisFrame();
    // Michael edit (special-shot): arm input check.
    // public bool WasSpecialShotPressedThisFrame() => specialShotAction != null && specialShotAction.action.WasPressedThisFrame();
    public bool TrueShotInProgress() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.IsInProgress() && shootSilverAction.action.IsInProgress();
    // public bool TrueShotReleasedThisFrame() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.WasReleasedThisFrame() && shootSilverAction.action.IsInProgress() || shootSilverAction.action.WasReleasedThisFrame() && shootIronAction.action.IsInProgress();
    // Michael edit (special-shot): bracketed so the null checks cover both releases.
    public bool AnyShotReleasedThisFrame() => shootIronAction != null && shootSilverAction != null && (shootIronAction.action.WasReleasedThisFrame() || shootSilverAction.action.WasReleasedThisFrame());

    private void Update()
    {
        AnyInput();
        // Michael edit (special-shot): evaluated every frame so the state machine never misses a press or release.
        GetShotIntent();
    }

    // Michael edit (special-shot): resolves the shot input for this frame. Cached per frame so every caller gets the same answer.
    // One gesture gives one shot: a single press fires a normal shot, holding both then releasing both fires the Special Shot.
    public ShotIntent GetShotIntent()
    {
        if (intentFrame == Time.frameCount) return currentIntent;
        intentFrame = Time.frameCount;
        currentIntent = ShotIntent.None;

        if (shootIronAction == null || shootSilverAction == null) return currentIntent;

        if (!canShoot)
        {
            shotState = ShotInputState.Idle;
            return currentIntent;
        }

        bool ironHeld = shootIronAction.action.IsPressed();
        bool silverHeld = shootSilverAction.action.IsPressed();
        bool bothHeld = ironHeld && silverHeld;
        bool noneHeld = !ironHeld && !silverHeld;

        switch (shotState)
        {
            case ShotInputState.Idle:
                if (bothHeld)
                {
                    shotState = ShotInputState.Charging;
                }
                else if (ironHeld || silverHeld)
                {
                    pendingType = ironHeld ? WeakPointType.Iron : WeakPointType.Silver;
                    float window = SpecialShotAvailable ? specialShotBuffer : 0f;

                    if (window <= 0f)
                    {
                        currentIntent = ToIntent(pendingType);
                        shotState = ShotInputState.Single;
                    }
                    else
                    {
                        pendingTimer = window;
                        shotState = ShotInputState.Pending;
                    }
                }
                break;

            case ShotInputState.Pending:
                pendingTimer -= Time.deltaTime;

                if (bothHeld)
                {
                    // other button came down in the window, becomes a charge
                    shotState = ShotInputState.Charging;
                }
                else if (noneHeld)
                {
                    // quick tap, fire now instead of waiting out the window
                    currentIntent = ToIntent(pendingType);
                    shotState = ShotInputState.Idle;
                }
                else if (pendingTimer <= 0f)
                {
                    currentIntent = ToIntent(pendingType);
                    shotState = ShotInputState.Single;
                }
                break;

            case ShotInputState.Single:
                // shot already fired, wait for everything to be released
                if (noneHeld) shotState = ShotInputState.Idle;
                break;

            case ShotInputState.Charging:
                if (noneHeld)
                {
                    currentIntent = ShotIntent.Special;
                    shotState = ShotInputState.Idle;
                }
                break;
        }

        return currentIntent;
    }

    private static ShotIntent ToIntent(WeakPointType type) => type == WeakPointType.Iron ? ShotIntent.Iron : ShotIntent.Silver;

    public void InputLock(bool enabled) // if InputLock(true) is called, it disables all movement from the player reader
    {
        canShoot = !enabled;
    }
    public bool AnyInput()
    {
        if (shootIronAction.action.IsPressed()) return true;
        else if (shootSilverAction.action.IsPressed()) return true;
        else if (reloadAction.action.IsPressed()) return true;
        // Michael edit (special-shot): include arm input, null-checked since it's optional.
        else if (specialShotAction != null && specialShotAction.action.IsPressed()) return true;
        else return false;
    }
}