using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private string ironActionName = "ShootIron";
    [SerializeField] private string silverActionName = "ShootSilver";
    [SerializeField] private string reloadActionName = "Reload";

    private InputAction shootIronAction;
    private InputAction shootSilverAction;
    private InputAction reloadAction;

    private void Awake()
    {
        shootIronAction = InputSystem.actions.FindAction(ironActionName);
        shootSilverAction = InputSystem.actions.FindAction(silverActionName);
        reloadAction = InputSystem.actions.FindAction(reloadActionName);
    }

    public bool WasIronPressedThisFrame() => shootIronAction != null && shootIronAction.WasPressedThisFrame();
    public bool WasSilverPressedThisFrame() => shootSilverAction != null && shootSilverAction.WasPressedThisFrame();
    public bool WasReloadPressedThisFrame() => reloadAction != null && reloadAction.WasPressedThisFrame();
}
