using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class PlayerDash : MonoBehaviour
{
    public bool dashEnabled = true;
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


    [Header("Cooldown Arrow UI")]
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
    [Tooltip("Duration of the fade-out after the arrow display (seconds).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("FOV Changes")]
    [Tooltip("Camera to modify. If null, Camera.main will be used.")]
    [SerializeField] private Camera playerCamera = null;
    [Tooltip("How many degrees to increase the camera FOV while dashing (added to current FOV).")]
    [SerializeField] private float dashFovIncrease = 12f;
    [Tooltip("How fast to lerp the camera FOV (higher = faster).")]
    [SerializeField] private float fovLerpSpeed = 8f;

    // Michael edit: replaced static Image with Volume-driven dash effects
    [Header("Dash Effects")]
    [Tooltip("Volume containing the DashEffectsVolumeComponent. If null, searches the scene.")]
    [SerializeField] private Volume dashEffectsVolume = null;
    [Tooltip("How quickly the dash effects reach full intensity when dashing (seconds).")]
    [SerializeField] private float dashEffectsFadeIn = 0.05f;
    [Tooltip("How quickly the dash effects fade out after a dash ends (seconds).")]
    [SerializeField] private float dashEffectsFadeOut = 0.2f;

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
    [Tooltip("Time in seconds for the bar to go from 0 -> full via passive recharge.")]
    [SerializeField] private float secondsToFullCharge = 30f;
    [Tooltip("Fraction of the full bar to add on a weakpoint hit (e.g. 0.2 = +20% of full bar).")]
    [SerializeField][Range(0f, 1f)] private float weakpointRechargeBonus = 0.2f;

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
    // normalized 0..1 charge value (1 == full)
    private float currentDashCharges = 0f;

    // reference to weapon events for listening to shot results
    private WeaponEvents weaponEvents = null;

    // Michael edit: cached volume component reference and current fade value
    private DashEffectsVolumeComponent dashEffectsComponent = null;
    private float dashEffectsCurrent = 0f;

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
        // resolve UI references if not assigned (UI lives on SceneEssentialsBundle)
        ResolveUIReferences();

        if (!dashEnabled)
        {
            arrowContainer.SetActive(false);
            chargeBarContainer.SetActive(false);
        }

        // initialize charges
        if (maxDashCharges < 1)
            maxDashCharges = 1;
        // start full (normalized)
        currentDashCharges = 1f;
        UpdateChargeUI();

        // Try to find WeaponEvents to subscribe for shot results so we can grant charges on weakpoint hits
        weaponEvents = GetComponent<WeaponEvents>();
        if (weaponEvents == null)
            weaponEvents = Object.FindFirstObjectByType<WeaponEvents>();

        if (weaponEvents != null)
            weaponEvents.ShotResolved += OnShotResolved;

        // Michael edit: resolve volume reference and cache the component
        if (dashEffectsVolume == null)
            dashEffectsVolume = Object.FindAnyObjectByType<Volume>();

        if (dashEffectsVolume != null)
            dashEffectsVolume.sharedProfile.TryGet(out dashEffectsComponent);

        // ensure effect starts off
        if (dashEffectsComponent != null)
            dashEffectsComponent.intensity.Override(0f);
    }

    private void OnDestroy()
    {
        if (weaponEvents != null)
            weaponEvents.ShotResolved -= OnShotResolved;

        // Michael edit: zero out the effect so it doesn't persist after player is destroyed
        if (dashEffectsComponent != null)
            dashEffectsComponent.intensity.Override(0f);
    }

    private void OnShotResolved(ShotResult result)
    {
        if (!dashUsesCharges)
            return;

        if (result.Outcome == ShotOutcome.WeakPointHit)
        {
            // give a fractional bonus to the normalized charge bar (for example, 0.2 = +20% of the full bar)
            AddDashChargeFraction(weakpointRechargeBonus);
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
        float needed = ChargePerDash;
        if (dashInput && !dashHeldLastFrame && !isDashing && dashCooldownTimer <= 0f && (!dashUsesCharges || currentDashCharges >= needed - 0.0001f))
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
                // consume a charge if using the charge-based dash (consume fractional amount)
                if (dashUsesCharges)
                {
                    currentDashCharges = Mathf.Max(0f, currentDashCharges - needed);
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
        if (!dashUsesCharges)
        {
            chargeBarContainer.SetActive(false);
        }

        if (dashUsesCharges)
        {
            arrowContainer.SetActive(false);
        }

        // Charges dash now has a passive recharge: normalized 0 to 1, increases toward 1 over secondsToFullCharge
        if (dashUsesCharges)
        {
            if (currentDashCharges < 1f && secondsToFullCharge > 0f)
            {
                currentDashCharges = Mathf.Clamp01(currentDashCharges + (Time.deltaTime / secondsToFullCharge));
            }
        }

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
                // Fade complete and disable images
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

        // Michael edit: fade dash effects intensity toward target via volume component
        UpdateDashEffects();

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

        // Update charge UI each frame (reflect normalized value)
        UpdateChargeUI();
    }

    // Michael edit: drives the volume component intensity based on dash state
    private void UpdateDashEffects()
    {
        if (dashEffectsComponent == null)
            return;

        float target = isDashing ? 1f : 0f;
        float fadeDuration = isDashing ? dashEffectsFadeIn : dashEffectsFadeOut;
        float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;

        dashEffectsCurrent = Mathf.MoveTowards(dashEffectsCurrent, target, step);
        dashEffectsComponent.intensity.Override(dashEffectsCurrent);
    }

    private void ResolveUIReferences()
    {
        if (arrowContainer == null)
        {
            GameObject dashArrow = GameObject.Find("DashArrow");
            if (dashArrow != null)
            {
                arrowContainer = dashArrow;
                if (dullArrow == null) dullArrow = dashArrow.transform.Find("DullArrow")?.GetComponent<Image>();
                if (fullArrow == null) fullArrow = dashArrow.transform.Find("FullArrow")?.GetComponent<Image>();
            }
        }

        if (chargeBarContainer == null)
        {
            GameObject chargeBarObj = GameObject.Find("ChargeBar");
            if (chargeBarObj != null)
            {
                chargeBarContainer = chargeBarObj;
                if (chargeBar == null) chargeBar = chargeBarObj.transform.Find("BarCharge")?.GetComponent<Image>();
            }
        }
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

        float add = amount * ChargePerDash;
        currentDashCharges = Mathf.Clamp01(currentDashCharges + add);
        UpdateChargeUI();
    }

    // Adds a fraction of the full bar
    public void AddDashChargeFraction(float amountNormalized = 1f)
    {
        if (amountNormalized <= 0f)
            return;

        currentDashCharges = Mathf.Clamp01(currentDashCharges + amountNormalized);
        UpdateChargeUI();
    }

    private void UpdateChargeUI()
    {
        if (chargeBar == null)
            return;

        chargeBar.fillAmount = Mathf.Clamp01(currentDashCharges);
    }

    private float ChargePerDash => 1f / Mathf.Max(1, maxDashCharges);

    public void DashVersionEnabled(string version)
    {
        dashEnabled = true;
        if (version == "charges")
        {
            dashUsesCharges = true;
            chargeBarContainer.SetActive(true);
            arrowContainer.SetActive(false);
        }
        else
        {
            arrowContainer.SetActive(true);
            chargeBarContainer.SetActive(false);
        }
    }
}