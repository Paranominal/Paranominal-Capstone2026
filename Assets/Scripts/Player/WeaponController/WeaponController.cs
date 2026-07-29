using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

// Summary: Ranged weapon system. Handles input, state, ammo, hitscan, hit resolution,
// and shot orchestration. Reads stats from the active RangedWeaponDefinition so new guns are data, not code.
// EDIT (weapon system): now uses RangedWeaponDefinition instead of WeaponDefinition.
public class WeaponController : MonoBehaviour
{
    // ---- Configuration ----

    [Header("Weapon")]
    [SerializeField] private RangedWeaponDefinition activeWeapon;

    [Header("References")]
    [SerializeField] private GunVisuals gunVisuals;
    [SerializeField] private CameraRecoilController cameraRecoilController;

    [Header("Hitscan")]
    [SerializeField] private LayerMask weakPointLayer;

    [Header("Input Actions")]
    [SerializeField] private string ironActionName = "ShootIron";
    [SerializeField] private string silverActionName = "ShootSilver";
    [SerializeField] private string reloadActionName = "Reload";

    // ---- Events ----
    // Consumers (WeaponAudio, PlayerHUD) subscribe to these the same way they did on WeaponEvents.

    public event Action<WeakPointType> ShotFired;
    public event Action<WeakPointType, bool> ShotResolved;
    public event Action<int, int> AmmoChanged;
    public event Action ReloadStarted;
    public event Action<float> ReloadProgressChanged;
    public event Action ReloadFinished;

    // ---- Public Properties ----

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => activeWeapon != null ? activeWeapon.magazineSize : 0;
    public bool IsReloading => isReloading;
    public bool IsOnCooldown => onCooldown;
    public bool IsWeaponEnabled => weaponEnabled;
    public bool IsIronBarrelEnabled => weaponEnabled && ironBarrelEnabled;
    public bool IsSilverBarrelEnabled => weaponEnabled && silverBarrelEnabled;
    public bool AutoReloadEnabled => weaponEnabled && autoReloadEnabled;
    // EDIT (weapon system): typed as RangedWeaponDefinition.
    public RangedWeaponDefinition ActiveWeapon => activeWeapon;

    public float ReloadProgress
    {
        get
        {
            if (!isReloading || activeWeapon == null || activeWeapon.reloadDuration <= 0f) return 0f;
            return Mathf.Clamp01(1f - (reloadTimeRemaining / activeWeapon.reloadDuration));
        }
    }

    // ---- Private State ----

    // Weapon state (was WeaponStateController)
    private bool weaponEnabled = true;
    private bool ironBarrelEnabled = true;
    private bool silverBarrelEnabled = true;
    private bool autoReloadEnabled = true;
    private bool isWeaponActive;
    private bool cachedIronBarrelEnabled = true;
    private bool cachedSilverBarrelEnabled = true;

    // Ammo state (was WeaponFiringLogic)
    private int currentAmmo;
    private bool isReloading;
    private float reloadTimeRemaining;
    private bool onCooldown;

    // Input (was WeaponInputReader)
    private InputAction shootIronAction;
    private InputAction shootSilverAction;
    private InputAction reloadAction;

    // Hitscan (was WeaponHitscan)
    private Raycaster raycaster;

    // Orchestration tracking (was ShotOrchestrator)
    private bool wasReloading;

    // ---- Lifecycle ----

    private void Awake()
    {
        shootIronAction = InputSystem.actions.FindAction(ironActionName);
        shootSilverAction = InputSystem.actions.FindAction(silverActionName);
        reloadAction = InputSystem.actions.FindAction(reloadActionName);

        if (cameraRecoilController == null) cameraRecoilController = FindAnyObjectByType<CameraRecoilController>();
        if (raycaster == null) raycaster = FindAnyObjectByType<Raycaster>();

        if (activeWeapon != null)
            InitFromDefinition(activeWeapon);
    }

    private void Start()
    {
        // EDIT (weapon manager): if no weapon is assigned at start, disable weapon
        // and wait for WeaponManager to equip one.
        if (activeWeapon == null)
        {
            weaponEnabled = false;
            isWeaponActive = false;
            return;
        }

        isWeaponActive = !weaponEnabled;
        SetWeaponEnabled(weaponEnabled);

        AmmoChanged?.Invoke(currentAmmo, MagazineSize);
    }

    private void Update()
    {
        // Reload progress tracking: raise events on state transitions.
        if (isReloading)
        {
            if (!wasReloading)
                ReloadStarted?.Invoke();

            ReloadProgressChanged?.Invoke(ReloadProgress);
            wasReloading = true;
            return;
        }

        if (wasReloading)
        {
            ReloadFinished?.Invoke();
            AmmoChanged?.Invoke(currentAmmo, MagazineSize);
            wasReloading = false;
        }

        if (!weaponEnabled) return;

        bool ironPressed = ironBarrelEnabled && shootIronAction != null && shootIronAction.WasPressedThisFrame();
        bool silverPressed = silverBarrelEnabled && shootSilverAction != null && shootSilverAction.WasPressedThisFrame();
        bool reloadPressed = reloadAction != null && reloadAction.WasPressedThisFrame();

        if (reloadPressed && CanManualReload())
        {
            TryStartReload();
            return;
        }

        if (onCooldown) return;
        if (!ironPressed && !silverPressed) return;

        WeakPointType shotType = ironPressed ? WeakPointType.Iron : WeakPointType.Silver;

        if (!HasAmmo() && autoReloadEnabled)
        {
            TryStartReload();
            return;
        }

        if (!HasAmmo()) return;

        StartShotCooldown();

        bool rewardedShot = Fire(shotType);
        if (!rewardedShot)
            ConsumeAmmo();

        ShotFired?.Invoke(shotType);
        ShotResolved?.Invoke(shotType, rewardedShot);
        AmmoChanged?.Invoke(currentAmmo, MagazineSize);

        if (!HasAmmo() && autoReloadEnabled)
            StartCoroutine(DelayedAutoReload());
    }

    // ---- Weapon Switching ----

    // Summary: Equip a new weapon definition. Resets ammo and state to match the new weapon's stats.
    // EDIT (weapon system): now takes RangedWeaponDefinition.
    public void EquipWeapon(RangedWeaponDefinition weapon)
    {
        EquipWeapon(weapon, gunVisuals);
    }

    // Summary: Equip a weapon with a specific GunVisuals reference. Used by WeaponManager
    // when activating a ranged weapon slot for the first time.
    // EDIT (weapon system): now takes RangedWeaponDefinition.
    public void EquipWeapon(RangedWeaponDefinition weapon, GunVisuals visuals)
    {
        CancelActiveState();
        activeWeapon = weapon;
        gunVisuals = visuals;

        if (weapon != null)
        {
            InitFromDefinition(weapon);
            isWeaponActive = false;   // force SetWeaponEnabled to run
            SetWeaponEnabled(true);
            AmmoChanged?.Invoke(currentAmmo, MagazineSize);
        }
    }

    // EDIT (weapon system): Resume a previously equipped weapon with cached ammo state.
    // Used by WeaponManager when re-equipping a holstered weapon. Does not reset ammo.
    // If mid-reload was interrupted, the weapon will need a manual reload.
    public void ResumeWeapon(RangedWeaponDefinition weapon, GunVisuals visuals, int cachedAmmo)
    {
        CancelActiveState();
        activeWeapon = weapon;
        gunVisuals = visuals;

        if (weapon != null)
        {
            currentAmmo = cachedAmmo;
            ironBarrelEnabled = weapon.ironBarrelAvailable;
            silverBarrelEnabled = weapon.silverBarrelAvailable;
            autoReloadEnabled = weapon.autoReload;
            cachedIronBarrelEnabled = ironBarrelEnabled;
            cachedSilverBarrelEnabled = silverBarrelEnabled;

            isWeaponActive = false;
            SetWeaponEnabled(true);
            AmmoChanged?.Invoke(currentAmmo, MagazineSize);
        }
    }

    // Summary: Unequips the current weapon. Hides visuals and disables firing.
    // EDIT (weapon manager): added so WeaponManager can clear the active weapon.
    public void UnequipWeapon()
    {
        CancelActiveState();
        SetWeaponEnabled(false);
        activeWeapon = null;
        gunVisuals = null;
    }

    private void InitFromDefinition(RangedWeaponDefinition def)
    {
        currentAmmo = def.magazineSize;
        ironBarrelEnabled = def.ironBarrelAvailable;
        silverBarrelEnabled = def.silverBarrelAvailable;
        autoReloadEnabled = def.autoReload;
        cachedIronBarrelEnabled = ironBarrelEnabled;
        cachedSilverBarrelEnabled = silverBarrelEnabled;
    }

    // ---- Weapon State (was WeaponStateController) ----

    public void SetWeaponEnabled(bool enabled)
    {
        if (isWeaponActive == enabled)
            return;

        weaponEnabled = enabled;

        if (enabled)
        {
            ironBarrelEnabled = cachedIronBarrelEnabled;
            silverBarrelEnabled = cachedSilverBarrelEnabled;
            if (gunVisuals != null) gunVisuals.SetVisualsVisible(true);
        }
        else
        {
            cachedIronBarrelEnabled = ironBarrelEnabled;
            cachedSilverBarrelEnabled = silverBarrelEnabled;
            ironBarrelEnabled = false;
            silverBarrelEnabled = false;
            if (gunVisuals != null) gunVisuals.SetVisualsVisible(false);
        }

        isWeaponActive = enabled;
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

    // ---- Ammo & Reload (was WeaponFiringLogic) ----

    public bool HasAmmo() => currentAmmo > 0;

    public bool CanManualReload() => !isReloading && currentAmmo < MagazineSize;

    public void ConsumeAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - Mathf.Max(0, amount));
    }

    public bool TryStartReload()
    {
        if (isReloading || activeWeapon == null) return false;
        StartCoroutine(ReloadRoutine());
        return true;
    }

    private void StartShotCooldown()
    {
        if (onCooldown) return;
        StartCoroutine(ShotCooldownRoutine());
    }

    public void CancelActiveState()
    {
        StopAllCoroutines();
        isReloading = false;
        onCooldown = false;
        reloadTimeRemaining = 0f;
        wasReloading = false;
    }

    private IEnumerator ReloadRoutine()
    {
        if (isReloading || activeWeapon == null) yield break;

        isReloading = true;
        reloadTimeRemaining = activeWeapon.reloadDuration;

        while (reloadTimeRemaining > 0f)
        {
            reloadTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        currentAmmo = activeWeapon.magazineSize;
        reloadTimeRemaining = 0f;
        isReloading = false;
    }

    private IEnumerator ShotCooldownRoutine()
    {
        if (activeWeapon == null) yield break;
        onCooldown = true;
        // EDIT (weapon system): attackCooldown lives on the base WeaponDefinition now.
        yield return new WaitForSeconds(activeWeapon.attackCooldown);
        onCooldown = false;
    }

    private IEnumerator DelayedAutoReload()
    {
        if (activeWeapon == null) yield break;
        yield return new WaitForSeconds(activeWeapon.postShotReloadDelay);
        if (!HasAmmo())
            TryStartReload();
    }

    // ---- Firing & Hitscan (was ShotOrchestrator + WeaponHitscan + WeakPointResolver) ----

    private bool Fire(WeakPointType shotType)
    {
        if (cameraRecoilController != null)
            cameraRecoilController.PlayShotCameraRecoil();

        if (gunVisuals != null)
            gunVisuals.PlayShotVisuals(shotType);

        if (raycaster == null || activeWeapon == null) return false;

        Ray ray = raycaster.Ray;
        float range = activeWeapon.hitscanRange;

        // Priority 1: weak points
        if (Physics.Raycast(ray, out RaycastHit weakHit, range, weakPointLayer, QueryTriggerInteraction.Collide))
        {
            WeakPoint wp = weakHit.collider.GetComponent<WeakPoint>();
            if (wp == null) wp = weakHit.collider.GetComponentInParent<WeakPoint>();

            if (wp != null)
                return ResolveWeakPointHit(wp, shotType, weakHit.collider.name);
        }

        // Priority 2: shootable targets
        if (Physics.Raycast(ray, out RaycastHit targetHit, range, ~0, QueryTriggerInteraction.Collide))
        {
            ShootableTarget target = targetHit.collider.GetComponent<ShootableTarget>();
            if (target == null) target = targetHit.collider.GetComponentInParent<ShootableTarget>();

            if (target != null)
                return target.ResolveHit(shotType);

            // Priority 3: generic damageables
            IDamageable damageable = targetHit.collider.GetComponent<IDamageable>();
            if (damageable == null) damageable = targetHit.collider.GetComponentInParent<IDamageable>();

            // EDIT (weapon system): build a proper DamageInfo from the weapon's stats.
            if (damageable != null)
            {
                Vector3 hitDir = (targetHit.collider.transform.position - transform.position);
                hitDir.y = 0f;
                if (hitDir.sqrMagnitude > 0.0001f) hitDir.Normalize();

                DamageInfo info = new DamageInfo(
                    0,
                    targetHit.point,
                    hitDir,
                    gameObject,
                    activeWeapon.knockbackForce
                );
                damageable.TakeDamage(info);
                return false;
            }
        }

        return false;
    }

    private bool ResolveWeakPointHit(WeakPoint weakPoint, WeakPointType shotType, string colliderName)
    {
        if (weakPoint == null) return false;

        bool correctType = weakPoint.weakPointType == shotType;
        weakPoint.OnHit(shotType);

        bool isTough = weakPoint.IsTough;
        bool isWarded = weakPoint.IsWarded;

        if (!isWarded)
        {
            if (correctType && isTough)
            {
                Debug.Log("Successful tough weakpoint hit (ammo preserved)! " + colliderName +
                    " | Remaining shots: " + weakPoint.RemainingShotsToDestroy);
                return true;
            }

            if (correctType)
            {
                Debug.Log("Successful weakpoint hit (ammo preserved)! " + colliderName);
                return true;
            }
        }
        else if (correctType && isWarded)
        {
            Debug.Log("Weakpoint hit with correct type, but is warded. (ammo consumed)! " + colliderName);
            return false;
        }

        Debug.Log("Weakpoint hit, wrong shot type (ammo consumed). " + colliderName);
        return false;
    }
}