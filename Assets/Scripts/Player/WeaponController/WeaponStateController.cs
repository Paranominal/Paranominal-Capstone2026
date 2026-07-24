using UnityEngine;

public class WeaponStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunVisuals gunVisuals;

    [Header("State")]
    [SerializeField] private bool weaponEnabled = true;
    [SerializeField] private bool ironBarrelEnabled = true;
    [SerializeField] private bool silverBarrelEnabled = true;
    [SerializeField] private bool autoReloadEnabled = true;

    private bool cachedIronBarrelEnabled = true;
    private bool cachedSilverBarrelEnabled = true;
    private bool isWeaponActive;

    public bool IsWeaponEnabled => weaponEnabled;
    public bool IsIronBarrelEnabled => weaponEnabled && ironBarrelEnabled;
    public bool IsSilverBarrelEnabled => weaponEnabled && silverBarrelEnabled;
    public bool AutoReloadEnabled => weaponEnabled && autoReloadEnabled;

    public GameObject shotGun;

    private void Awake()
    {
        if (gunVisuals == null) gunVisuals = GetComponent<GunVisuals>();

        cachedIronBarrelEnabled = ironBarrelEnabled;
        cachedSilverBarrelEnabled = silverBarrelEnabled;
    }

    private void Start()
    {
        isWeaponActive = !weaponEnabled;
        SetWeaponEnabled(weaponEnabled);
    }

    public void SetWeaponEnabled(bool enabled)
    {
        if (isWeaponActive == enabled)
            return;

        weaponEnabled = enabled;

        if (enabled)
            EnableWeapon();
        else
            DisableWeapon();

        isWeaponActive = enabled;
    }

    public void EnableWeapon()
    {
        ironBarrelEnabled = cachedIronBarrelEnabled;
        silverBarrelEnabled = cachedSilverBarrelEnabled;

        if (gunVisuals != null)
            gunVisuals.SetVisualsVisible(true);        
    }

    public void DisableWeapon()
    {
        cachedIronBarrelEnabled = ironBarrelEnabled;
        cachedSilverBarrelEnabled = silverBarrelEnabled;
        ironBarrelEnabled = false;
        silverBarrelEnabled = false;

        if (gunVisuals != null)
            gunVisuals.SetVisualsVisible(false);
    }

    public void SetBarrelAvailability(bool ironEnabled, bool silverEnabled)
    {
        ironBarrelEnabled = ironEnabled;
        silverBarrelEnabled = silverEnabled;

        if (weaponEnabled)
        {
            cachedIronBarrelEnabled = ironEnabled;
            cachedSilverBarrelEnabled = silverEnabled;
        }
    }

    public void SetAutoReloadEnabled(bool enabled)
    {
        autoReloadEnabled = enabled;
    }
}
