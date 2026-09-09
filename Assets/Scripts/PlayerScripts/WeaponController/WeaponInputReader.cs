using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInputReader : MonoBehaviour
{
    [Header("Input Actions")]

    [SerializeField] private InputActionReference shootIronAction;
    [SerializeField] private InputActionReference shootSilverAction;
    [SerializeField] private InputActionReference reloadAction;
    public bool canShoot = true;
    public bool CanShoot => canShoot;

    public bool WasIronPressedThisFrame() => shootIronAction != null && shootIronAction.action.WasPressedThisFrame();
    public bool WasSilverPressedThisFrame() => shootSilverAction != null && shootSilverAction.action.WasPressedThisFrame();
    public bool WasReloadPressedThisFrame() => reloadAction != null && reloadAction.action.WasPressedThisFrame();

    
    private void Update()
    {
        AnyInput();
    }
    public bool AnyInput()
    {
        if (shootIronAction.action.IsPressed()) return true;
        else if (shootSilverAction.action.IsPressed()) return true;
        else if (reloadAction.action.IsPressed()) return true;
        else return false;
    }
}
