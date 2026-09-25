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
    private bool canShoot = true;
    public bool CanShoot => canShoot;

    public bool WasIronPressedThisFrame() => shootIronAction != null && shootIronAction.action.WasPressedThisFrame();
    public bool WasSilverPressedThisFrame() => shootSilverAction != null && shootSilverAction.action.WasPressedThisFrame();
    public bool WasReloadPressedThisFrame() => reloadAction != null && reloadAction.action.WasPressedThisFrame();
    // EDIT (special-shot): arm input check.
    public bool WasSpecialShotPressedThisFrame() => specialShotAction != null && specialShotAction.action.WasPressedThisFrame();

    
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
