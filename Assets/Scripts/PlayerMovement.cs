using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // main first-person camera used for look rotation and FOV effects
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    // additional overlay cameras (weakpoints/UI layers, etc) that must match base camera FOV
    // so stacked rendering stays aligned during dash FOV changes. had issues with weakpoints and ui elements moving during dash
    [SerializeField] private Camera[] linkedFovCameras;

    // name of the Input System action.
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference dashAction;

    // moving and looking tuning
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float lookXLimit = 60f;
    [SerializeField] private float lookSmoothing = 0.03f;

    private Vector2 smoothedLookDelta;
    private Vector2 lookDeltaVelocity;

    // Dash tuning:
    // - distance: how far miriam travels backwards
    // - duration: how long it takes to dash
    // - cooldown: how long before she can dash again
    // - curve: allows for tuning dash non-linearly, feels good but can be adjusted later
    [Header("Dash")]
    [SerializeField] private float dashDistance = 2.5f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.75f;
    [SerializeField]
    private AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.8f),
        new Keyframe(0.25f, 1.1f),
        new Keyframe(1f, 0f)
    );

    // Visual feedback for dash impact:
    // - small camera pitch impulse on dash start to enhance feel of quick movement
    // - temporary FOV boost and smooth return, again makes it feel impactful but can be adjusted
    [Header("Dash Feel")]
    [SerializeField] private float dashCameraPitchImpulse = 1.2f;
    [SerializeField] private float dashFovBoost = 6f;
    [SerializeField] private float dashFovRecoverSpeed = 20f;

    // how long it takes for gun to return to rest visually, not tied to reload or cooldowns
    [Header("Recoil")]
    [SerializeField] private float recoilReturnTime = 0.08f;

    // required movement component for collisions
    private CharacterController characterController;

    // Camera local X rotation in degrees
    private float cameraPitch;
    // Global movement lock toggle (future-proofed for pause/cutscenes?)
    public bool canMove = true;

    // Recoil state:
    // recoilOffsetX is additive camera pitch offset from shooting/dash effects,
    // recoilVelocityX is required for smoothing (SmoothDamp) to return camera to baseline after recoil
    private float recoilOffsetX;
    private float recoilVelocityX;

    // Dash state flags
    private bool isDashing;
    private bool dashOnCooldown;
    private float dashCooldownRemaining;
    public bool IsDashOnCooldown => dashOnCooldown;
    public float DashCooldownProgress
    {
        get
        {
            if (!dashOnCooldown || dashCooldown <= 0f) return 1f; // full = ready
            float remaining01 = Mathf.Clamp01(dashCooldownRemaining / dashCooldown);
            return 1f - remaining01;
        }
    }

    // base and target FOV used for dash FOV feedback transitions, cached from the camera at start so dash can always return to the correct baseline even if designers change the default FOV later
    private float baseFov;
    private float targetFov;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            // cache initial FOV so dash effects always return to the authored baseline
            baseFov = playerCamera.fieldOfView;
            targetFov = baseFov;
        }
    }

    private void Update()
    {
        // early out when movement is globally disabled (pausing mid dash, dashing it cutscene future-proofing, etc)
        if (!canMove)
            return;

        // dash input is processed first so dash can override regular movement this frame
        HandleDashInput();

        // while dashing, movement comes from the dash coroutine only. prevents player moving during dash
        if (!isDashing)
            HandleMovement();

        // look and camera feedback are always updated each frame
        HandleLook();
        UpdateDashCameraFeedback();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction != null && moveAction.action != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        Vector3 move = (transform.forward * moveInput.y + transform.right * moveInput.x) * walkSpeed;
        characterController.Move(move * Time.deltaTime);
    }

    private void HandleDashInput()
    {
        // dash triggers on action press and is blocked when already dashing or cooling down
        if (dashAction != null && dashAction.action != null &&
            dashAction.action.WasPressedThisFrame() &&
            !isDashing && !dashOnCooldown)
        {
            StartCoroutine(DashBackwardRoutine());
        }
    }

    private IEnumerator DashBackwardRoutine()
    {
        // enter dash state
        isDashing = true;
        dashOnCooldown = true;

        // dash direction is always backward relative to current direction faced
        Vector3 dashDirection = -transform.forward;
        float elapsed = 0f;

        // apply immediate camera feedback
        recoilOffsetX += dashCameraPitchImpulse;
        targetFov = baseFov + dashFovBoost;

        // move for duration of dash, shaping speed by curve across normalized time.
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, dashDuration));
            float speedFactor = Mathf.Max(0f, dashSpeedCurve.Evaluate(t));

            float speed = (dashDistance / Mathf.Max(0.01f, dashDuration)) * speedFactor;
            characterController.Move(dashDirection * speed * Time.deltaTime);

            yield return null;
        }

        // end dash movement and start restoring FOV
        isDashing = false;
        targetFov = baseFov;

        // dash cooldown prevents more dashing
        dashCooldownRemaining = dashCooldown;
        while (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
            yield return null;
        }
        dashCooldownRemaining = 0f;
        dashOnCooldown = false;
    }

    // handles to looking around with input action system
    private void HandleLook()
    {
        Vector2 rawLookInput = lookAction != null && lookAction.action != null
            ? lookAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        // when changed from old script, had issues with camera jittering, added smoothing (can be tuned)
        smoothedLookDelta = Vector2.SmoothDamp(
            smoothedLookDelta,
            rawLookInput,
            ref lookDeltaVelocity,
            lookSmoothing
        );

        float mouseX = smoothedLookDelta.x * lookSensitivity;
        float mouseY = smoothedLookDelta.y * lookSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);

        recoilOffsetX = Mathf.SmoothDamp(recoilOffsetX, 0f, ref recoilVelocityX, recoilReturnTime);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch + recoilOffsetX, 0f, 0f);

        transform.Rotate(0f, mouseX, 0f);
    }

    public void AddVerticalRecoil(float upDegrees)
    {
        // negative offset rotates camera up in this implementation, visual recoil for camera
        recoilOffsetX -= Mathf.Abs(upDegrees);
    }

    private void UpdateDashCameraFeedback()
    {
        if (playerCamera == null)
            return;

        float newFov = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFov,
            dashFovRecoverSpeed * Time.deltaTime
        );

        // keep base camera and linked overlay cameras in sync to avoid visual separation in camera stacks
        playerCamera.fieldOfView = newFov;

        if (linkedFovCameras == null)
            return;

        for (int i = 0; i < linkedFovCameras.Length; i++)
        {
            Camera cam = linkedFovCameras[i];
            if (cam != null)
                cam.fieldOfView = newFov;
        }
    }
}