using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInputReader : MonoBehaviour
{
    [Header("Input Actions")]

    [SerializeField] private InputActionReference shootIronAction;
    [SerializeField] private InputActionReference shootSilverAction;
    [SerializeField] private InputActionReference reloadAction;
    // EDIT (special-shot): input to arm the Special Shot.
    [SerializeField] private InputActionReference specialShotAction;
    [Tooltip("The ammount of time in seconds that the game waits to see if the player wants to charge a special shot")]
    [SerializeField] private float specialShotBuffer;
    private bool canShoot = true;
    public bool CanShoot => canShoot;

    private float buffer;
    public bool WasIronPressedThisFrame() => shootIronAction != null && shootIronAction.action.WasPerformedThisFrame() && !shootSilverAction.action.IsInProgress();
    public bool WasSilverPressedThisFrame() => shootSilverAction != null && shootSilverAction.action.WasPerformedThisFrame() && !shootIronAction.action.IsInProgress();
    public bool WasReloadPressedThisFrame() => reloadAction != null && reloadAction.action.WasPressedThisFrame();
    // EDIT (special-shot): arm input check.
    // public bool WasSpecialShotPressedThisFrame() => specialShotAction != null && specialShotAction.action.WasPressedThisFrame();
    public bool TrueShotInProgress() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.IsInProgress() && shootSilverAction.action.IsInProgress();
    public bool TrueShotCompletedThisFrame() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.WasCompletedThisFrame() && shootSilverAction.action.IsInProgress() || shootSilverAction.action.WasCompletedThisFrame() && shootIronAction.action.IsInProgress();
    // public bool TrueShotReleasedThisFrame() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.WasReleasedThisFrame() && shootSilverAction.action.IsInProgress() || shootSilverAction.action.WasReleasedThisFrame() && shootIronAction.action.IsInProgress();
    public bool AnyShotReleasedThisFrame() => shootIronAction != null && shootSilverAction != null && shootIronAction.action.WasReleasedThisFrame()|| shootSilverAction.action.WasReleasedThisFrame();

    
    private void Update()
    {
        AnyInput();
    }

    public void InputLock(bool enabled) // if InputLock(true) is called, it disables all movement from the player reader
    {
        canShoot = !enabled;
    }
    public bool AnyInput()
    {
        if (shootIronAction.action.IsPressed()) return true;
        else if (shootSilverAction.action.IsPressed()) return true;
        else if (reloadAction.action.IsPressed()) return true;
        // EDIT (special-shot): include arm input, null-checked since it's optional.
        else if (specialShotAction != null && specialShotAction.action.IsPressed()) return true;
        else return false;
    }
}
