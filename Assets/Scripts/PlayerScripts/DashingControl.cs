using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Dashing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera[] linkedFovCameras;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference dashAction;

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

    [Header("Dash Feel")]
    [SerializeField] private float dashCameraPitchImpulse = 1.2f;
    [SerializeField] private float dashFovBoost = 6f;
    [SerializeField] private float dashFovRecoverSpeed = 20f;

    private CharacterController characterController;

    private bool isDashing;
    private bool dashOnCooldown;
    private float dashCooldownRemaining;

    private float baseFov;
    private float targetFov;

    public bool IsDashing => isDashing;
    public bool IsDashOnCooldown => dashOnCooldown;
    public float DashCooldownProgress
    {
        get
        {
            if (!dashOnCooldown || dashCooldown <= 0f) return 1f;
            float remaining01 = Mathf.Clamp01(dashCooldownRemaining / dashCooldown);
            return 1f - remaining01;
        }
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;

        if (playerCamera != null)
        {
            baseFov = playerCamera.fieldOfView;
            targetFov = baseFov;
        }
    }

    private void Update()
    {
        if (playerMovement != null && !playerMovement.CanMove)
            return;

        HandleDashInput();
        UpdateDashCameraFeedback();
    }

    private void HandleDashInput()
    {
        if (dashAction != null && dashAction.action != null &&
            dashAction.action.WasPressedThisFrame() &&
            !isDashing && !dashOnCooldown)
        {
            StartCoroutine(DashBackwardRoutine());
        }
    }

    private IEnumerator DashBackwardRoutine()
    {
        isDashing = true;
        dashOnCooldown = true;

        Vector3 dashDirection = -transform.forward;
        float elapsed = 0f;

        if (playerMovement != null)
            playerMovement.AddPitchOffset(dashCameraPitchImpulse);

        targetFov = baseFov + dashFovBoost;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, dashDuration));
            float speedFactor = Mathf.Max(0f, dashSpeedCurve.Evaluate(t));

            float speed = (dashDistance / Mathf.Max(0.01f, dashDuration)) * speedFactor;
            characterController.Move(dashDirection * speed * Time.deltaTime);

            yield return null;
        }

        isDashing = false;
        targetFov = baseFov;

        dashCooldownRemaining = dashCooldown;
        while (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
            yield return null;
        }

        dashCooldownRemaining = 0f;
        dashOnCooldown = false;
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
