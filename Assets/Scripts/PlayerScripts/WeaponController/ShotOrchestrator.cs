using UnityEngine;

public class ShotOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponInputReader weaponInputReader;
    [SerializeField] private WeaponFiringLogic weaponFiringLogic;
    [SerializeField] private WeaponHitscan weaponHitscan;
    [SerializeField] private WeakPointResolver weakPointResolver;
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private CameraRecoilController cameraRecoilController;
    [SerializeField] private GunVisuals gunVisuals;

    [Header("Shot Settings")]
    [SerializeField] private bool autoReload = true;
    [SerializeField] private bool silverBarrelEnabled = true;
    [SerializeField] private bool ironBarrelEnabled = true;

    private bool wasReloading;

    private void Awake()
    {
        if (weaponInputReader == null) weaponInputReader = GetComponent<WeaponInputReader>();
        if (weaponFiringLogic == null) weaponFiringLogic = GetComponent<WeaponFiringLogic>();
        if (weaponHitscan == null) weaponHitscan = GetComponent<WeaponHitscan>();
        if (weakPointResolver == null) weakPointResolver = GetComponent<WeakPointResolver>();
        if (weaponEvents == null) weaponEvents = GetComponent<WeaponEvents>();
        if (cameraRecoilController == null) cameraRecoilController = GetComponent<CameraRecoilController>();
        if (gunVisuals == null) gunVisuals = GetComponent<GunVisuals>();

        if (weaponFiringLogic != null && weaponEvents != null)
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
    }

    private void Update()
    {
        if (weaponInputReader == null || weaponFiringLogic == null)
            return;

        if (weaponFiringLogic.IsReloading)
        {
            if (!wasReloading && weaponEvents != null)
                weaponEvents.RaiseReloadStarted();

            if (weaponEvents != null)
                weaponEvents.RaiseReloadProgressChanged(weaponFiringLogic.ReloadProgress);

            wasReloading = true;
            return;
        }

        if (wasReloading)
        {
            if (weaponEvents != null)
            {
                weaponEvents.RaiseReloadFinished();
                weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
            }

            wasReloading = false;
        }

        bool ironPressed = ironBarrelEnabled && weaponInputReader.WasIronPressedThisFrame();
        bool silverPressed = silverBarrelEnabled && weaponInputReader.WasSilverPressedThisFrame();
        bool reloadPressed = weaponInputReader.WasReloadPressedThisFrame();

        if (reloadPressed && weaponFiringLogic.CanManualReload())
        {
            weaponFiringLogic.TryStartReload();
            return;
        }

        if (weaponFiringLogic.IsOnCooldown)
            return;

        if (!ironPressed && !silverPressed)
            return;

        WeakPointType shotType = ironPressed ? WeakPointType.Iron : WeakPointType.Silver;

        if (!weaponFiringLogic.HasAmmo() && autoReload)
        {
            weaponFiringLogic.TryStartReload();
            return;
        }

        if (!weaponFiringLogic.HasAmmo())
            return;

        weaponFiringLogic.StartShotCooldown();

        bool rewardedShot = Fire(shotType);
        if (!rewardedShot)
            weaponFiringLogic.ConsumeAmmo();

        if (weaponEvents != null)
        {
            weaponEvents.RaiseShotFired(shotType);
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
        }

        if (!weaponFiringLogic.HasAmmo() && autoReload)
            weaponFiringLogic.TryStartReload();
    }

    private bool Fire(WeakPointType shotType)
    {
        if (cameraRecoilController != null)
            cameraRecoilController.PlayShotCameraRecoil();

        if (gunVisuals != null)
            gunVisuals.PlayShotVisuals(shotType);

        if (weaponHitscan == null)
            return false;

        if (weaponHitscan.TryGetWeakPointHit(out WeakPoint weakPoint, out RaycastHit hitWeak))
        {
            if (weakPointResolver != null)
                return weakPointResolver.ResolveWeakPointHit(weakPoint, shotType, hitWeak.collider.name);

            return false;
        }

        weaponHitscan.LogWorldHitOrMiss();
        return false;
    }
}
