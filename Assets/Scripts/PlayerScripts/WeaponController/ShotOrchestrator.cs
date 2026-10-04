using UnityEngine;
using System.Collections;
// Michael edit (special-shot): needed for the Special Shot's hit list.
using System.Collections.Generic;
using System;

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
    // Michael edit (special-shot): optional, leave empty on weapons without a Special Shot.
    [SerializeField] private SpecialShot specialShot;

    [Header("Reload")]
    [SerializeField] private float postShotReloadDelay = 0.25f;

    private bool wasReloading;
    // Michael edit (special-shot): a Special Shot released during the shot/misfire cooldown fires as soon as the weapon is free.
    private bool queuedSpecialShot;
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
        // Michael edit (special-shot): fallback for the Special Shot reference.
        if (specialShot == null) specialShot = GetComponent<SpecialShot>();

        if (weaponFiringLogic != null && weaponEvents != null)
            weaponEvents.RaiseAmmoChanged(weaponFiringLogic.CurrentAmmo, weaponFiringLogic.MagazineSize);
    }

    private void Update()
    {

        if (weaponInputReader == null || weaponFiringLogic == null)
            return;

        // Michael edit (special-shot): input is resolved by WeaponInputReader. It only buffers presses while the Special Shot is ready.
        weaponInputReader.SpecialShotAvailable = specialShot != null && specialShot.IsReady;
        ShotIntent intent = weaponInputReader.GetShotIntent();

        if (!weaponInputReader.CanShoot) return;

        if (weaponFiringLogic.IsReloading)
        {
            // Michael edit (special-shot): Special Shot is blocked during reload, same as normal shots.
            queuedSpecialShot = false;

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
        {
            queuedSpecialShot = false; // Michael edit (special-shot)
            return;
        }

        bool reloadPressed = weaponInputReader.WasReloadPressedThisFrame();

        if (reloadPressed && !IsWeaponBusy && weaponFiringLogic.CanManualReload())
        {
            queuedSpecialShot = false; // Michael edit (special-shot)
            weaponFiringLogic.TryStartReload();
            return;
        }

        // Michael edit (special-shot): queue a released Special Shot so it isn't lost if the weapon is still on cooldown.
        if (intent == ShotIntent.Special && specialShot != null && specialShot.IsReady)
            queuedSpecialShot = true;

        if (IsWeaponBusy)
            return;

        // Michael edit (special-shot): fire the queued Special Shot once the weapon is free.
        if (queuedSpecialShot)
        {
            queuedSpecialShot = false;
            if (specialShot != null && specialShot.IsReady)
                FireSpecialShot();
            return;
        }

        // Michael edit (special-shot): normal shots come from the resolved intent, filtered by barrel state.
        bool ironEnabled = weaponStateController == null || weaponStateController.IsIronBarrelEnabled;
        bool silverEnabled = weaponStateController == null || weaponStateController.IsSilverBarrelEnabled;

        if (intent == ShotIntent.Iron && ironEnabled)
            FireNormalShot(WeakPointType.Iron);
        else if (intent == ShotIntent.Silver && silverEnabled)
            FireNormalShot(WeakPointType.Silver);
    }

    // Michael edit (special-shot): normal shot flow, moved out of Update so buffered shots can use it. Logic unchanged.
    private void FireNormalShot(WeakPointType shotType)
    {
        bool autoReloadEnabled = weaponStateController == null || weaponStateController.AutoReloadEnabled;

        if (!weaponFiringLogic.HasAmmo() && autoReloadEnabled)
        {
            weaponFiringLogic.TryStartReload();
            return;
        }

        if (!weaponFiringLogic.HasAmmo())
            return;
        
        if (gunVisuals) gunVisuals.DoGunFX(shotType); 
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
            // EDIT (projectile-shot-types): shot type travels with the damage so damageables can filter by it.
            ShotTypeMask shotMask = shotType.ToShotMask();

            // EDIT (projectile-shot-types): projectiles only break to their allowed shot types, anything else is a misfire.
            if (damageable is Projectile projectile)
            {
                if (!projectile.CanBeDestroyedBy(shotMask))
                    return BuildResult(shotType, ShotOutcome.WrongAmmo, damageHit.point);

                projectile.TakeDamage(new DamageInfo(0, damageHit.point, transform.forward, gameObject, shotMask));
                return BuildResult(shotType, ShotOutcome.EnemyHit, damageHit.point);
            }

            bool wasStaggered = damageable is EnemyStagger stagger && stagger.IsStaggered;

            //define damage info
            DamageInfo info = new DamageInfo(0, damageHit.point, transform.forward, gameObject, shotMask); // EDIT (projectile-shot-types)
            damageable.TakeDamage(info);

            // damageable.TakeDamage(new DamageInfo());
            return BuildResult(shotType, wasStaggered ? ShotOutcome.EnemyHitStaggered : ShotOutcome.EnemyHit, damageHit.point);
        }

        return BuildResult(shotType, ShotOutcome.Miss, weaponHitscan.LogWorldHitOrMiss());
    }

    // Michael edit (special-shot): fires the Special Shot. Normal cooldown, no ammo cost, no misfire penalty.
    // One ShotResolved is raised per target hit, or a single SpecialMiss if nothing was hit.
    private void FireSpecialShot()
    {
        // consume first so the streak ignores this shot's own results
        specialShot.TryArm();
        specialShot.Consume();
        weaponFiringLogic.StartShotCooldown();

        if (gunVisuals != null)
        {
            gunVisuals.PlayShotVisuals(WeakPointType.Special);
            if (gunVisuals) gunVisuals.DoTrueShotFX(); 
        }

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

    // Michael edit (special-shot): applies the piercing shot to everything along the ray.
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
            // EDIT (projectile-shot-types): Special projectiles are destroyed, others are passed through untouched. No result is raised either way.
            Projectile projectile = hit.collider.GetComponentInParent<Projectile>();
            if (projectile != null)
            {
                if (projectile.CanBeDestroyedBy(ShotTypeMask.Special))
                    projectile.TakeDamage(new DamageInfo(0, hit.point, weaponHitscan.AimRay.direction, gameObject, ShotTypeMask.Special));
                continue;
            }

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
