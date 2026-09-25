using UnityEngine;
using System.Collections;
// EDIT (special-shot): needed for the Special Shot's hit list.
using System.Collections.Generic;

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
    // EDIT (special-shot): optional, leave empty on weapons without a Special Shot.
    [SerializeField] private SpecialShot specialShot;

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
        // EDIT (special-shot): fallback for the Special Shot reference.
        if (specialShot == null) specialShot = GetComponent<SpecialShot>();

        if (weaponFiringLogic != null && weaponEvents != null)
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
    }

    private void Update()
    {

        if (weaponInputReader == null || weaponFiringLogic == null)
            return;
        if (!weaponInputReader.CanShoot) return;

        // EDIT (special-shot): arming is checked before reload handling so it works mid-reload.
        HandleSpecialShotArming();

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

        // EDIT (special-shot): an armed Special Shot replaces the next shot. It costs no ammo, so it skips the ammo checks.
        if (specialShot != null && specialShot.IsArmed)
        {
            FireSpecialShot();
            return;
        }

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
            if (!result.Outcome.RetainsAmmo())
                weaponFiringLogic.ConsumeAmmo();

            if (weaponEvents != null)
            {
                weaponEvents.RaiseShotFired(shotType);
                weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
                weaponEvents.RaiseShotResolved(result);
            }

            if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
            {
                weaponFiringLogic.StartShotCooldown();
                StartCoroutine(DelayedAutoReload());
            }
            else
            {
                weaponFiringLogic.StartMisfireCooldown();
                isMisfireEffectsActive = true;
                StartCoroutine(DelayedMisfireVisuals());

                if (autoReloadEnabled)
                    StartCoroutine(DelayedAutoReload());
            }
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

        return BuildResult(shotType, ShotOutcome.Miss, weaponHitscan.LogWorldHitOrMiss());
    }

    // EDIT (special-shot): arms a Ready Special Shot on input. Once armed it can't be cancelled.
    private void HandleSpecialShotArming()
    {
        if (specialShot == null || !specialShot.IsReady) return;
        if (weaponStateController != null && !weaponStateController.IsWeaponEnabled) return;

        if (weaponInputReader.WasSpecialShotPressedThisFrame())
            specialShot.TryArm();
    }

    // EDIT (special-shot): fires the Special Shot. Normal cooldown, no ammo cost, no misfire penalty.
    // One ShotResolved is raised per target hit, or a single SpecialMiss if nothing was hit.
    private void FireSpecialShot()
    {
        // consume first so the streak ignores this shot's own results
        specialShot.Consume();
        weaponFiringLogic.StartShotCooldown();

        if (gunVisuals != null)
            gunVisuals.PlayShotVisuals(WeakPointType.Special);

        if (cameraRecoilController != null)
            cameraRecoilController.PlayShotCameraRecoil();

        List<ShotResult> results = ResolveSpecialShot();

        if (weaponEvents != null)
        {
            weaponEvents.RaiseShotFired(WeakPointType.Special);
            foreach (ShotResult result in results)
                weaponEvents.RaiseShotResolved(result);
        }
    }

    // EDIT (special-shot): applies the piercing shot to everything along the ray.
    // Special weakpoints are destroyed, other weakpoints are passed through, enemies are killed or staggered (see Enemy.HandleSpecialShotHit).
    private List<ShotResult> ResolveSpecialShot()
    {
        List<ShotResult> results = new List<ShotResult>();

        if (weaponHitscan == null)
        {
            results.Add(BuildResult(WeakPointType.Special, ShotOutcome.SpecialMiss, Vector3.zero));
            return results;
        }

        List<RaycastHit> hits = weaponHitscan.GetSpecialShotHits(out Vector3 missPoint);
        HashSet<Enemy> checkedEnemies = new HashSet<Enemy>();   // body already tested this shot
        HashSet<Enemy> resolvedEnemies = new HashSet<Enemy>();  // already produced a SpecialHit this shot
        HashSet<WeakPoint> hitWeakPoints = new HashSet<WeakPoint>();

        foreach (RaycastHit hit in hits)
        {
            WeakPoint weakPoint = hit.collider.GetComponentInParent<WeakPoint>();
            if (weakPoint != null)
            {
                if (!weakPoint.IsSpecial || weakPoint.IsWarded || !hitWeakPoints.Add(weakPoint))
                    continue;

                // skip if the owner already died to this shot
                Enemy owner = weakPoint.GetComponentInParent<Enemy>();
                if (owner != null && owner.IsDying)
                    continue;

                // stops the owner's body counting as a second hit
                if (owner != null) resolvedEnemies.Add(owner);

                weakPoint.OnHit(WeakPointType.Special);
                results.Add(BuildResult(WeakPointType.Special, ShotOutcome.SpecialHit, hit.point, 1f, weakPoint.OwnerCentre));
                continue;
            }

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy == null || resolvedEnemies.Contains(enemy) || !checkedEnemies.Add(enemy))
                continue;

            if (enemy.HandleSpecialShotHit())
            {
                resolvedEnemies.Add(enemy);
                results.Add(BuildResult(WeakPointType.Special, ShotOutcome.SpecialHit, hit.point, 1f, enemy.transform.position));
            }
        }

        if (results.Count == 0)
            results.Add(BuildResult(WeakPointType.Special, ShotOutcome.SpecialMiss, missPoint));

        return results;
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
