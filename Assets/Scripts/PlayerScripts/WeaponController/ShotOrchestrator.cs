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
    [SerializeField] private WeaponStateController weaponStateController;

    [Header("Reload")]
    [SerializeField] private float postShotReloadDelay = 0.25f;

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
        if (weaponStateController == null) weaponStateController = GetComponent<WeaponStateController>();

        if (weaponFiringLogic != null && weaponEvents != null)
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
    }

    private void Update()
    {
        if (weaponInputReader == null || weaponFiringLogic == null)
            return;
        if (!weaponInputReader.canShoot) return;

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

        if (weaponStateController != null && !weaponStateController.IsWeaponEnabled)
            return;

        bool ironPressed = (weaponStateController == null || weaponStateController.IsIronBarrelEnabled) && weaponInputReader.WasIronPressedThisFrame();
        bool silverPressed = (weaponStateController == null || weaponStateController.IsSilverBarrelEnabled) && weaponInputReader.WasSilverPressedThisFrame();
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

        bool autoReloadEnabled = weaponStateController == null || weaponStateController.AutoReloadEnabled;

        if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
        {
            weaponFiringLogic.TryStartReload();
            return;
        }

        if (!weaponFiringLogic.HasAmmo())
            return;

        weaponFiringLogic.StartShotCooldown();

        ShotResult result = Fire(shotType);
        if (!result.Outcome.RetainsAmmo())
            weaponFiringLogic.ConsumeAmmo();

        if (weaponEvents != null)
        {
            weaponEvents.RaiseShotFired(shotType);
            weaponEvents.RaiseShotResolved(result);
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
        }

        if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
            StartCoroutine(DelayedAutoReload());
    }

    private ShotResult BuildResult(WeakPointType shotType, ShotOutcome outcome, Vector3 hitPoint, float accuracy = 0f)
    {
        return new ShotResult
        {
            ShotType = shotType,
            Outcome = outcome,
            Accuracy = accuracy,
            HitPoint = hitPoint
        };
    }

    private ShotResult Fire(WeakPointType shotType)
    {
        if (cameraRecoilController != null)
            cameraRecoilController.PlayShotCameraRecoil();

        if (gunVisuals != null)
            gunVisuals.PlayShotVisuals(shotType);

        if (weaponHitscan == null)
            return BuildResult(shotType, ShotOutcome.Miss, Vector3.zero);

        if (weaponHitscan.TryGetWeakPointHit(out WeakPoint weakPoint, out RaycastHit hitWeak))
        {
            if (weakPointResolver == null)
                return BuildResult(shotType, ShotOutcome.Miss, hitWeak.point);

            ShotOutcome outcome = weakPointResolver.ResolveWeakPointHit(weakPoint, shotType, hitWeak.collider.name);
            return BuildResult(shotType, outcome, hitWeak.point, weakPoint.GetAccuracy(weaponHitscan.AimRay));
        }

        if (weaponHitscan.TryGetShootableTargetHit(out ShootableTarget target, out RaycastHit targetHit))
        {
            ShotOutcome outcome = target.ResolveHit(shotType) ? ShotOutcome.ShootableTargetHit : ShotOutcome.WrongAmmo;
            return BuildResult(shotType, outcome, targetHit.point);
        }

        if (weaponHitscan.TryGetDamageableHit(out IDamageable damageable, out RaycastHit damageHit))
        {
            // shifted so capturing stagger state BEFORE damage
            bool wasStaggered = damageable is EnemyStagger stagger && stagger.IsStaggered;

            damageable.TakeDamage(new DamageInfo());

            return BuildResult(shotType, wasStaggered ? ShotOutcome.EnemyHitStaggered : ShotOutcome.EnemyHit, damageHit.point);
        }

        weaponHitscan.LogWorldHitOrMiss();
        return BuildResult(shotType, ShotOutcome.Miss, Vector3.zero);

    }

    private System.Collections.IEnumerator DelayedAutoReload()
    {
        yield return new WaitForSeconds(postShotReloadDelay);

        // Re-check ammo in case something else refilled it during the delay
        if (!weaponFiringLogic.HasAmmo())
            weaponFiringLogic.TryStartReload();
    }
}
