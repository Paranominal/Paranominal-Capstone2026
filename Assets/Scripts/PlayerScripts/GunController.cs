using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    // input action names from the Input System
    [Header("Input Actions")]
    [SerializeField] private string ironActionName = "ShootIron";
    [SerializeField] private string silverActionName = "ShootSilver";
    [SerializeField] private string reloadActionName = "Reload";

    // core references for shooting logic
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GunVisuals gunVisuals;
    // layer mask used for weakpoint-priority raycast
    [SerializeField] private LayerMask WeakPoint;

    // Shot flow tuning for designers:
    // - shotCooldown: delay between allowed shots
    // - magazineSize/reloadDuration: simple magazine + auto-reload model
    // - rayDistance: max hit distance
    [Header("Shot Settings")]
    [SerializeField] private bool autoReload = true;
    [SerializeField] private bool silverBarrelEnabled = true;
    [SerializeField] private bool ironBarrelEnabled = true;
    [SerializeField] private float shotCooldown = 0.2f;
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private float reloadDuration = 2f;
    [SerializeField] private float rayDistance = 1000f;

    // cached input actions
    private InputAction shootIronAction;
    private InputAction shootSilverAction;
    private InputAction reloadAction;

    // Weapon state flags and ammo for flow control and UI
    private bool onCooldown;
    private bool isReloading;
    private int currentAmmo;

    private float reloadTimeRemaining;

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
        if (gunVisuals == null) gunVisuals = GetComponent<GunVisuals>();

        // Resolve input actions by name one time to ensure existence
        shootIronAction = InputSystem.actions.FindAction(ironActionName);
        shootSilverAction = InputSystem.actions.FindAction(silverActionName);
        reloadAction = InputSystem.actions.FindAction(reloadActionName);

        // Start with full magazine 
        currentAmmo = magazineSize;
    }

    private void Update()
    {
        // blocks all input while actively reloading
        if (isReloading)
            return;

        // Read one-frame button presses from Input System
        bool ironPressed = shootIronAction != null && shootIronAction.WasPressedThisFrame() && ironBarrelEnabled;
        bool silverPressed = shootSilverAction != null && shootSilverAction.WasPressedThisFrame() && silverBarrelEnabled;
        bool reloadPressed = reloadAction != null && reloadAction.WasPressedThisFrame();

        // manual reload (only if not already full)
        if (reloadPressed && currentAmmo < magazineSize)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        // keep existing fire cooldown behavior
        if (onCooldown)
            return;

        if (!ironPressed && !silverPressed)
            return;

        // If both happen in the same frame, iron takes priority.
        WeakPointType shotType = ironPressed ? WeakPointType.Iron : WeakPointType.Silver;

        // Auto-reload if empty and do not fire this frame
        if (currentAmmo <= 0 && autoReload)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        // Fire if ammo is available
        if (currentAmmo > 0)
        {
            onCooldown = true;

            bool rewardedShot = Fire(shotType);

            // punish misses/invalid weakpoint shots by consuming ammo
            if (!rewardedShot)
                currentAmmo--;

            StartCoroutine(ShotCooldownRoutine());

            // reload when magazine is depleted by this shot
            if (currentAmmo <= 0 && autoReload)
                StartCoroutine(ReloadRoutine());
        }
    }

    private bool Fire(WeakPointType shotType)
    {
        if (gunVisuals != null)
            gunVisuals.PlayShotVisuals(shotType);

        if (playerCamera == null)
            return false;

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        // Weakpoint-only pass
        bool hasWeakHit = Physics.Raycast(ray, out RaycastHit hitWeak, rayDistance, WeakPoint, QueryTriggerInteraction.Collide);

        if (hasWeakHit)
        {
            WeakPoint weakPoint = hitWeak.collider.GetComponent<WeakPoint>();
            if (weakPoint == null)
                weakPoint = hitWeak.collider.GetComponentInParent<WeakPoint>();

            if (weakPoint != null)
            {
                bool correctType = weakPoint.weakPointType == shotType;
                weakPoint.OnHit(shotType);

                if (correctType)
                {
                    Debug.Log("Successful weakpoint hit (ammo preserved)! " + hitWeak.collider.name);
                    return true;
                }

                Debug.Log("Weakpoint hit, wrong shot type (ammo consumed). " + hitWeak.collider.name);
                return false;
            }
        }

        // world/body hit = miss for reward purposes
        if (Physics.Raycast(ray, out RaycastHit hitAny, rayDistance, ~0, QueryTriggerInteraction.Collide))
            Debug.Log("Hit! " + hitAny.collider.name);
        else
            Debug.Log("Miss...");

        return false;
    }

    private IEnumerator ShotCooldownRoutine()
    {
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