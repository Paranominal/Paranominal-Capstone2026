using UnityEngine;
using UnityEngine.UI;

public class PlayerDash : MonoBehaviour
{
    [Tooltip("Horizontal dash speed applied while dashing.")]
    [SerializeField] private float dashSpeed = 15f;
    [Tooltip("Duration of the dash in seconds.")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("Allow dashing while airborne. If false, dash can only start when grounded.")]
    [SerializeField] private bool allowAirDash = false;
    [Tooltip("Allow the player to jump during a dash. When enabled, jumping while dashing launches the player into the air and ends the dash.")]
    [SerializeField] private bool allowDashJump = false;
    [Tooltip("Cooldown after a dash before another dash can be started.")]
    [SerializeField] private float dashCooldown = 1f;


    [Header("UI")]
    [Tooltip("Dull arrow image that is shown faded while dash is on cooldown.")]
    [SerializeField] private Image dullArrow = null;
    [Tooltip("Full arrow image that is filled bottom->top to indicate cooldown progress.")]
    [SerializeField] private Image fullArrow = null;
    [SerializeField] private GameObject arrowContainer = null;
    [Tooltip("Alpha for the dull arrow while cooldown is active.")]
    [Range(0f, 1f)]
    [SerializeField] private float dullFadeAlpha = 0.5f;
    [Tooltip("How long to keep the full arrow visible once the cooldown completes (seconds).")]
    [SerializeField] private float showFullAfterCooldownSeconds = 1f;
    [SerializeField] private Image speedLines = null;
    [Tooltip("Duration of the fade-out after the arrow display (seconds).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("FOV Changes")]
    [Tooltip("Camera to modify. If null, Camera.main will be used.")]
    [SerializeField] private Camera playerCamera = null;
    [Tooltip("How many degrees to increase the camera FOV while dashing (added to current FOV).")]
    [SerializeField] private float dashFovIncrease = 12f;
    [Tooltip("How fast to lerp the camera FOV (higher = faster).")]
    [SerializeField] private float fovLerpSpeed = 8f;

    [Header("Charge Dash")]
    [Tooltip("When enabled, dash consumes charges instead of using the normal cooldown behaviour.")]
    [SerializeField] private bool dashUsesCharges = false;
    [Tooltip("Maximum number of dash charges the player can hold.")]
    [SerializeField] private int maxDashCharges = 3;
    [Tooltip("Short cooldown applied when using charges (seconds).")]
    [SerializeField] private float chargeDashCooldown = 0.5f;
    [Tooltip("UI fill image representing dash charges (fillAmount = charges / maxCharges).")]
    [SerializeField] private Image chargeBar = null;
    [SerializeField] private GameObject chargeBarContainer = null;

    // UI state
    private bool cooldownActive = false;
    private float postFullTimer = 0f;
    private bool isFadingOut = false;
    private float fadeTimer = 0f;
    private float startAlphaDull = 0f;
    private float startAlphaFull = 0f;

    // Dash state control variables
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;
    private bool dashHeldLastFrame = false;
    private float dashCooldownTimer = 0f;
    private int currentDashCharges = 0;

    // reference to weapon events for listening to shot results
    private WeaponEvents weaponEvents = null;

    // FOV handling
    private Camera cam = null;
    private bool fovActive = false; // whether we are currently lerping FOV
    private float preDashFov = 60f;
    private float desiredFov = 60f;

    public bool IsDashing => isDashing;
    public Vector3 CurrentDashVelocity => isDashing ? dashDirection * dashSpeed : Vector3.zero;
    public bool AllowDashJump => allowDashJump;

    private void Awake()
    {
        if (!dashUsesCharges)
        {
            chargeBarContainer.SetActive(false);
        }

        if (dashUsesCharges)
        {
            arrowContainer.SetActive(false);
        }


        // initialize charges
        if (maxDashCharges < 1)
            maxDashCharges = 1;
        currentDashCharges = maxDashCharges;
        UpdateChargeUI();

        // Try to find WeaponEvents to subscribe for shot results so we can grant charges on weakpoint hits
        weaponEvents = GetComponent<WeaponEvents>();
        if (weaponEvents == null)
            weaponEvents = Object.FindFirstObjectByType<WeaponEvents>();

        if (weaponEvents != null)
            weaponEvents.ShotResolved += OnShotResolved;
    }

    private void OnDestroy()
    {
        if (weaponEvents != null)
            weaponEvents.ShotResolved -= OnShotResolved;
    }

    private void OnShotResolved(ShotResult result)
    {
        if (!dashUsesCharges)
            return;

        if (result.Outcome == ShotOutcome.WeakPointHit)
        {
            AddDashCharge(1);
        }
    }

    public void HandleDashInput(bool dashInput, Vector3 desiredDirection, Transform transform, CharacterController characterController, float deltaTime)
    {
        // Tick cooldown timer
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= deltaTime;

        // track cooldown state transitions so we can show the full-arrow for a short time
        bool wasCooldownActive = cooldownActive;
        cooldownActive = dashCooldownTimer > 0f;
        if (wasCooldownActive && !cooldownActive)
        {
            // cooldown just finished, so show full arrow for a short time
            postFullTimer = showFullAfterCooldownSeconds;
        }

        // Handle dash start (pressed this frame)
        if (dashInput && !dashHeldLastFrame && !isDashing && dashCooldownTimer <= 0f && (!dashUsesCharges || currentDashCharges > 0))
        {
            // Only allow starting a dash when grounded unless air dashing is enabled
            if (!characterController.isGrounded && !allowAirDash)
            {
                // cannot start dash in air
            }
            else
            {
                // if player is giving movement input use that for desired direction, otherwise send them forward
                if (desiredDirection.sqrMagnitude > 0.01f)
                    dashDirection = desiredDirection.normalized;
                else
                    dashDirection = transform.forward;

                isDashing = true;
                dashTimer = dashDuration;
                // consume a charge if using the charge-based dash
                if (dashUsesCharges)
                {
                    currentDashCharges = Mathf.Max(0, currentDashCharges - 1);
                    UpdateChargeUI();
                }
                StartDashFOV();
            }
        }

        dashHeldLastFrame = dashInput;

        // Update dash timer
        if (isDashing)
        {
            dashTimer -= deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                EndDashFOV();
                // start cooldown (shorter when using charges)
                dashCooldownTimer = dashUsesCharges ? chargeDashCooldown : dashCooldown;
            }
        }
    }

    public void CancelDashAndStartCooldown()
    {
        if (isDashing)
        {
            isDashing = false;
            EndDashFOV();
            dashCooldownTimer = dashUsesCharges ? chargeDashCooldown : dashCooldown;
        }
    }

    private void Update()
    {
        // Update post-full display timer and detect transition to start fading
        bool wasPostActive = postFullTimer > 0f;
        if (postFullTimer > 0f)
            postFullTimer -= Time.deltaTime;
        bool isPostActiveNow = postFullTimer > 0f;

        if (wasPostActive && !isPostActiveNow)
        {
            // start fade-out when the post-full hold ends
            StartFadeOut();
        }

        // Update fade-out if active
        if (isFadingOut)
        {
            fadeTimer -= Time.deltaTime;

            if (cooldownActive)
            {
                // If cooldown restarted while fading, cancel fade and restore visuals for cooldown
                isFadingOut = false;
                SetImageAlpha(dullArrow, dullFadeAlpha);
                // Ensure full arrow is visible again when cooldown is active
                SetImageAlpha(fullArrow, 1f);
                dullArrow.gameObject.SetActive(true);
                fullArrow.gameObject.SetActive(true);
            }
            else if (fadeTimer <= 0f)
            {
                // Fade complete: disable images
                isFadingOut = false;
                SetImageAlpha(dullArrow, 0f);
                SetImageAlpha(fullArrow, 0f);
                dullArrow.gameObject.SetActive(false);
                fullArrow.gameObject.SetActive(false);
            }
            else
            {
                float t = Mathf.Clamp01(fadeTimer / fadeOutDuration);
                SetImageAlpha(dullArrow, startAlphaDull * t);
                SetImageAlpha(fullArrow, startAlphaFull * t);
                // keep images active during fade
                dullArrow.gameObject.SetActive(true);
                fullArrow.gameObject.SetActive(true);
            }
        }

        UpdateDashUI();

        // Show speed lines while dashing
        if (speedLines != null)
            speedLines.enabled = isDashing;

        // Handle FOV interpolation
        if (cam == null)
            cam = playerCamera != null ? playerCamera : Camera.main;

        if (cam != null && fovActive)
        {
            // Smoothly lerp towards desired FOV
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFov, Time.deltaTime * fovLerpSpeed);

            // If we've returned to the pre-dash FOV and dash is finished, stop updating
            if (!isDashing && Mathf.Abs(cam.fieldOfView - desiredFov) < 0.01f && Mathf.Approximately(desiredFov, preDashFov))
            {
                cam.fieldOfView = preDashFov;
                fovActive = false;
            }
        }

        // Update charge UI each frame (instant jumps per requirement)
        UpdateChargeUI();
    }

    private void UpdateDashUI()
    {
        if (dullArrow == null || fullArrow == null)
            return;

        if (isFadingOut)
            return;

        if (cooldownActive)
        {
            // Show dull arrow faded and update fill from bottom->top as cooldown progresses
            SetImageAlpha(dullArrow, dullFadeAlpha);
            dullArrow.gameObject.SetActive(true);

            // Make sure full arrow is fully visible while filling
            SetImageAlpha(fullArrow, 1f);
            fullArrow.gameObject.SetActive(true);

            if (dashUsesCharges)
            {
                // When using charges, the cooldown fill is based on the short charge cooldown
                float denom = chargeDashCooldown <= 0f ? 1f : chargeDashCooldown;
                float fill = 1f - Mathf.Clamp01(dashCooldownTimer / denom);
                fullArrow.fillAmount = fill;
            } else
            {
                float denom = dashCooldown <= 0f ? 1f : dashCooldown;
                float fill = 1f - Mathf.Clamp01(dashCooldownTimer / denom);
                fullArrow.fillAmount = fill;
            }
            
        }


        else if (postFullTimer > 0f)
        {
            // Cooldown finished recently, show full arrow fully filled for a moment
            SetImageAlpha(dullArrow, dullFadeAlpha);
            dullArrow.gameObject.SetActive(true);

            // Ensure the full arrow is fully visible during the post-full hold
            SetImageAlpha(fullArrow, 1f);
            fullArrow.gameObject.SetActive(true);
            fullArrow.fillAmount = 1f;
        }
        else
        {
            // Nothing to show
            dullArrow.gameObject.SetActive(false);
            fullArrow.gameObject.SetActive(false);
        }
    }

    private void StartFadeOut()
    {
        if (dullArrow == null || fullArrow == null)
            return;

        isFadingOut = true;
        fadeTimer = fadeOutDuration;
        startAlphaDull = dullArrow.color.a;
        startAlphaFull = fullArrow.color.a;
        // ensure they are active at start of fade
        dullArrow.gameObject.SetActive(true);
        fullArrow.gameObject.SetActive(true);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null)
            return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private void StartDashFOV()
    {
        if (cam == null)
            cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
            return;

        // store the FOV that should be restored after dash
        preDashFov = cam.fieldOfView;
        desiredFov = preDashFov + dashFovIncrease;
        fovActive = true;
    }

    private void EndDashFOV()
    {
        if (cam == null)
            cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
            return;

        // When dash ends, lerp back to the saved pre-dash FOV
        desiredFov = preDashFov;
        fovActive = true;
    }
    // Adds dash charges
    public void AddDashCharge(int amount = 1)
    {
        if (amount <= 0)
            return;

        currentDashCharges = Mathf.Clamp(currentDashCharges + amount, 0, maxDashCharges);
        UpdateChargeUI();
    }

    private void UpdateChargeUI()
    {
        if (chargeBar == null)
            return;

        if (maxDashCharges <= 0)
        {
            chargeBar.fillAmount = 0f;
            return;
        }

        chargeBar.fillAmount = (float)currentDashCharges / (float)maxDashCharges;
    }
}
