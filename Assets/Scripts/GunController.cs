using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    // input action names from the Input System
    [Header("Input Actions")]
    [SerializeField] private string ironActionName = "ShootIron";
    [SerializeField] private string silverActionName = "ShootSilver";

    // core references for shooting and recoil visuals
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform gunModel;
    // seperating muzzle flashes, could allow for more in future
    [SerializeField] private SpriteRenderer ironMuzzleFlash;
    [SerializeField] private SpriteRenderer silverMuzzleFlash;
    // layer mask used for weakpoint-priority raycast
    [SerializeField] private LayerMask WeakPoint;

    // Shot flow tuning for designers:
    // - shotCooldown: delay between allowed shots
    // - magazineSize/reloadDuration: simple magazine + auto-reload model
    // - rayDistance: max hit distance
    [Header("Shot Settings")]
    [SerializeField] private float shotCooldown = 0.2f;
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private float reloadDuration = 2f;
    [SerializeField] private float rayDistance = 1000f;

    // recoil feedback tuning for camera and gun viewmodel
    [Header("Visual Recoil")]
    [SerializeField] private float cameraRecoilUpDegrees = 1.5f;
    [SerializeField] private Vector3 gunKickOffset = new Vector3(0f, 0.02f, -0.08f);
    [SerializeField] private float gunKickUpRotationDegrees = 4f;
    [SerializeField] private float gunKickTime = 0.05f;
    [SerializeField] private float gunReturnTime = 0.1f;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    // cached input actions
    private InputAction shootIronAction;
    private InputAction shootSilverAction;

    // Weapon state flags and ammo for flow control and UI
    private bool onCooldown;
    private bool isReloading;
    private int currentAmmo;

    private float reloadTimeRemaining;

    // Coroutine handles so repeated shots can restart visuals cleanly.
    private Coroutine muzzleFlashRoutine;
    private Coroutine gunKickRoutine;

    // UI Read-Only states
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
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
        // Fallback camera resolution for convenience in scene setup.
        if (playerCamera == null) playerCamera = Camera.main;

        // Resolve input actions by name one time to ensure existence
        shootIronAction = InputSystem.actions.FindAction(ironActionName);
        shootSilverAction = InputSystem.actions.FindAction(silverActionName);

        // Start with full magazine 
        currentAmmo = magazineSize;

        // keeps flashes hidden on startup
        if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
        if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;
    }

    private void Update()
    {
        // prevents firing during cooldowns and reloads
        if (isReloading || onCooldown)
            return;

        // Read one-frame button presses from Input System
        bool ironPressed = shootIronAction != null && shootIronAction.WasPressedThisFrame();
        bool silverPressed = shootSilverAction != null && shootSilverAction.WasPressedThisFrame();

        if (!ironPressed && !silverPressed)
            return;

        // If both happen in the same frame, iron takes priority.
        WeakPointType shotType = ironPressed ? WeakPointType.Iron : WeakPointType.Silver;

        // Auto-reload if empty and do not fire this frame. Manual reloading not added (yet?)
        if (currentAmmo <= 0)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        // Fire, consume ammo, and lock shot input for cooldown duration
        Fire(shotType);
        currentAmmo--;

        StartCoroutine(ShotCooldownRoutine());

        // reload when magazine is depleted by this shot
        if (currentAmmo <= 0)
            StartCoroutine(ReloadRoutine());
    }

    private void Fire(WeakPointType shotType)
    {
        PlayMuzzleFlash(shotType);
        PlayRecoil();

        if (playerCamera == null)
            return;

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        // Weakpoint-only pass: if aimed at a weakpoint layer collider, always treat as weakpoint hit. might need to be moved to weakpoint manager
        bool hasWeakHit = Physics.Raycast(ray, out RaycastHit hitWeak, rayDistance, WeakPoint, QueryTriggerInteraction.Collide);

        if (hasWeakHit)
        {
            WeakPoint weakPoint = hitWeak.collider.GetComponent<WeakPoint>();
            if (weakPoint == null)
                weakPoint = hitWeak.collider.GetComponentInParent<WeakPoint>();

            if (weakPoint != null)
            {
                weakPoint.OnHit(shotType);
                Debug.Log("Hit Weakpoint! " + hitWeak.collider.name);
                return;
            }
        }

        // pass for normal world/body hits (debug purposes, could be expanded with damageable targets and hit effects)
        if (Physics.Raycast(ray, out RaycastHit hitAny, rayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Debug.Log("Hit! " + hitAny.collider.name);
        }
        else
        {
            Debug.Log("Miss...");
        }
    }

    private void PlayMuzzleFlash(WeakPointType shotType)
    {
        // Restart flash when firing quickly so visuals stay responsive (only applicable if cooldown is short enough)
        if (muzzleFlashRoutine != null)
            StopCoroutine(muzzleFlashRoutine);

        // reset both before enabling the chosen type
        if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
        if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;

        SpriteRenderer target = shotType == WeakPointType.Iron ? ironMuzzleFlash : silverMuzzleFlash;
        if (target == null) return;

        muzzleFlashRoutine = StartCoroutine(MuzzleFlashRoutine(target));
    }

    // manages the flash duration timing and visibility toggle, separate from recoil for flexibility and to avoid coupling with shot cooldown
    private IEnumerator MuzzleFlashRoutine(SpriteRenderer target)
    {
        target.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        target.enabled = false;
    }

    private void PlayRecoil()
    {
        // Camera recoil is delegated to PlayerMovement so look/recoil (essentially camera adjustments) stay in one place
        if (playerMovement != null)
            playerMovement.AddVerticalRecoil(cameraRecoilUpDegrees);

        if (gunModel == null)
            return;

        // Restart kick animation on rapid fire instead of stacking coroutines (again, only applicable if cooldown is short enough to allow multiple shots within the animation duration)
        if (gunKickRoutine != null)
            StopCoroutine(gunKickRoutine);

        gunKickRoutine = StartCoroutine(GunKickRoutine());
    }

    private IEnumerator GunKickRoutine()
    {
        // capture current local transform as animation baseline
        Vector3 startPos = gunModel.localPosition;
        Quaternion startRot = gunModel.localRotation;

        Vector3 kickedPos = startPos + gunKickOffset;
        Quaternion kickedRot = startRot * Quaternion.Euler(-gunKickUpRotationDegrees, 0f, 0f);

        // move/rotate into recoil pose.
        float t = 0f;
        while (t < gunKickTime)
        {
            t += Time.deltaTime;
            float alpha = gunKickTime <= 0f ? 1f : Mathf.Clamp01(t / gunKickTime);

            gunModel.localPosition = Vector3.Lerp(startPos, kickedPos, alpha);
            gunModel.localRotation = Quaternion.Slerp(startRot, kickedRot, alpha);
            yield return null;
        }

        // return back to baseline pose.
        t = 0f;
        while (t < gunReturnTime)
        {
            t += Time.deltaTime;
            float alpha = gunReturnTime <= 0f ? 1f : Mathf.Clamp01(t / gunReturnTime);

            gunModel.localPosition = Vector3.Lerp(kickedPos, startPos, alpha);
            gunModel.localRotation = Quaternion.Slerp(kickedRot, startRot, alpha);
            yield return null;
        }

        gunModel.localPosition = startPos;
        gunModel.localRotation = startRot;
    }

    private IEnumerator ShotCooldownRoutine()
    {
        // shared cooldown for both fire types
        onCooldown = true;
        yield return new WaitForSeconds(shotCooldown);
        onCooldown = false;
    }

    private IEnumerator ReloadRoutine()
    {
        // guard against duplicate reload coroutines
        if (isReloading)
            yield break;

        isReloading = true;
        reloadTimeRemaining = reloadDuration;

        while (reloadTimeRemaining > 0f)
        {
            reloadTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        // instant refill after reload delay
        currentAmmo = magazineSize;
        reloadTimeRemaining = 0f;
        isReloading = false;
    }
}