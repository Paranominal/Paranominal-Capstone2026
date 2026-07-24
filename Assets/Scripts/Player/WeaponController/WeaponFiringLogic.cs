using UnityEngine;
using System.Collections;

public class WeaponFiringLogic : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private float reloadDuration = 2f;

    [Header("Shot Cooldown")]
    [SerializeField] private float shotCooldown = 0.2f;

    private bool isReloading;
    private bool onCooldown;
    private int currentAmmo;
    private float reloadTimeRemaining;

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public bool IsOnCooldown => onCooldown;
    public float ReloadProgress
    {
        get
        {
            if (!isReloading || reloadDuration <= 0f) return 0f;
            return Mathf.Clamp01(1f - (reloadTimeRemaining / reloadDuration));
        }
    }

    private void Awake()
    {
        currentAmmo = magazineSize;
    }

    public bool HasAmmo() => currentAmmo > 0;

    public bool CanManualReload() => !isReloading && currentAmmo < magazineSize;

    public void StartShotCooldown()
    {
        if (onCooldown)
            return;

        StartCoroutine(ShotCooldownRoutine());
    }

    public bool TryStartReload()
    {
        if (isReloading)
            return false;

        StartCoroutine(ReloadRoutine());
        return true;
    }

    public void ConsumeAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - Mathf.Max(0, amount));
    }

    private IEnumerator ReloadRoutine()
    {
        if (isReloading)
            yield break;

        isReloading = true;
        reloadTimeRemaining = reloadDuration;

        while (reloadTimeRemaining > 0f)
        {
            reloadTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        currentAmmo = magazineSize;
        reloadTimeRemaining = 0f;
        isReloading = false;
    }

    private IEnumerator ShotCooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(shotCooldown);
        onCooldown = false;
    }

    public void CancelActiveState()
    {
        StopAllCoroutines();
        isReloading = false;
        onCooldown = false;
        reloadTimeRemaining = 0f;
    }
}
