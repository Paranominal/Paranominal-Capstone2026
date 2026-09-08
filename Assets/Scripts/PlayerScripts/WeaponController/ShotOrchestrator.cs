using UnityEngine;
using System.Collections;

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
    private bool isMisfireEffectsActive;
    private bool IsWeaponBusy =>
        weaponFiringLogic.IsOnCooldown ||
        weaponFiringLogic.IsOnMisfireCooldown ||
        isMisfireEffectsActive;

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
            {
                weaponEvents.RaiseReloadStarted();
                // Play reload animation when reload starts
                if (gunVisuals != null)
                    gunVisuals.PlayReloadAnimation();
            }

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

        if (reloadPressed && !IsWeaponBusy && weaponFiringLogic.CanManualReload())
        {
            weaponFiringLogic.TryStartReload();
            return;
        }

        if (IsWeaponBusy)
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

        ShotResult result = Fire(shotType);
        bool isMisfire = result.Outcome == ShotOutcome.Miss || result.Outcome == ShotOutcome.WrongAmmo || result.Outcome == ShotOutcome.EnemyHitStaggered;

        if (isMisfire)
        {
            weaponFiringLogic.StartMisfireCooldown();
            isMisfireEffectsActive = true;

            if (!result.Outcome.RetainsAmmo())
                weaponFiringLogic.ConsumeAmmo();

            if (weaponEvents != null)
            {
                weaponEvents.RaiseShotFired(shotType);
                weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
                weaponEvents.RaiseShotResolved(result);
            }

            StartCoroutine(DelayedMisfireVisuals());

            if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
                StartCoroutine(DelayedAutoReload());
        }
        else
        {
            weaponFiringLogic.StartShotCooldown();
            if (!result.Outcome.RetainsAmmo())
                weaponFiringLogic.ConsumeAmmo();

            if (weaponEvents != null)
            {
                weaponEvents.RaiseShotFired(shotType);
                weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
                weaponEvents.RaiseShotResolved(result);
            }

            if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
                StartCoroutine(DelayedAutoReload());
        }
    }

    private ShotResult BuildResult(WeakPointType shotType, ShotOutcome outcome, Vector3 hitPoint, float accuracy = 0f, Vector3 ownerCentre = default)
    {
        return new ShotResult
        {
            ShotType = shotType,
            Outcome = outcome,
            Accuracy = accuracy,
            HitPoint = hitPoint,
            OwnerCentre = ownerCentre
        };
    }

    private ShotResult Fire(WeakPointType shotType)
    {
        // Plays shot visuals for all shots
        // Misfires will have additional effects played afterwards
        void shotVisuals()
        {
            if (gunVisuals != null)
                gunVisuals.PlayShotVisuals(shotType);

            if (cameraRecoilController != null)
                cameraRecoilController.PlayShotCameraRecoil();
        }

        shotVisuals();

        if (weaponHitscan == null)
            return BuildResult(shotType, ShotOutcome.Miss, Vector3.zero);

        if (weaponHitscan.TryGetWeakPointHit(out WeakPoint weakPoint, out RaycastHit hitWeak))
        {
            if (weakPointResolver == null)
                return BuildResult(shotType, ShotOutcome.Miss, hitWeak.point);
            ShotOutcome outcome = weakPointResolver.ResolveWeakPointHit(weakPoint, shotType, hitWeak.collider.name);
            return BuildResult(shotType, outcome, hitWeak.point, weakPoint.GetAccuracy(weaponHitscan.AimRay), weakPoint.OwnerCentre);
        }

        if (weaponHitscan.TryGetShootableTargetHit(out ShootableTarget target, out RaycastHit targetHit))
        {
            ShotOutcome outcome = target.ResolveHit(shotType) ? ShotOutcome.ShootableTargetHit : ShotOutcome.WrongAmmo;
            return BuildResult(shotType, outcome, targetHit.point);
        }

        if (weaponHitscan.TryGetDamageableHit(out IDamageable damageable, out RaycastHit damageHit))
        {
            bool wasStaggered = damageable is EnemyStagger stagger && stagger.IsStaggered;
            damageable.TakeDamage(new DamageInfo());
            return BuildResult(shotType, wasStaggered ? ShotOutcome.EnemyHitStaggered : ShotOutcome.EnemyHit, damageHit.point);
        }

        weaponHitscan.LogWorldHitOrMiss();
        return BuildResult(shotType, ShotOutcome.Miss, Vector3.zero);
    }

    private IEnumerator DelayedAutoReload()
    {
        yield return new WaitForSeconds(postShotReloadDelay);

        while (IsWeaponBusy)
            yield return null;

        if (!weaponFiringLogic.HasAmmo())
            weaponFiringLogic.TryStartReload();
    }

    private IEnumerator DelayedMisfireVisuals()
    {
        // shouldn't be using magic number, but this is just the amount of time the shotgun shot sound plays because they use the same audio source, they tend to overlap without it
        yield return new WaitForSeconds(0.3f);

        if (weaponEvents != null)
            weaponEvents.RaiseMisfired();

        if (gunVisuals != null)
            gunVisuals.PlayMisfireVisuals();

        // Keep reload blocked until the misfire texture/animation has fully played out
        float remaining = gunVisuals != null ? gunVisuals.GetMisfireVisualsDuration() : 0f;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        isMisfireEffectsActive = false;
    }
}
