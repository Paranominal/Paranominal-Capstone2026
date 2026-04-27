using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // main first-person camera used for look rotation and FOV effects
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    // name of the Input System action.
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;

    // moving and looking tuning
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    // Global movement lock toggle (future-proofed for pause/cutscenes?)
    public bool canMove = true; // hi i need to change this value from the grimoire (and likely other stuff in future) so the read-only version won't work well
    public bool CanMove => canMove;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float lookXLimit = 60f;
    [SerializeField] private float lookSmoothing = 0.03f;

    private Vector2 smoothedLookDelta;
    private Vector2 lookDeltaVelocity;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;
    private float verticalVelocity;

    // how long it takes for gun to return to rest visually, not tied to reload or cooldowns
    [Header("Camera Recoil")]
    [SerializeField] private float recoilReturnTime = 0.08f;

    // required movement component for collisions
    private CharacterController characterController;
    private Dashing dashing;

    // Camera local X rotation in degrees
    private float cameraPitch;

    // Recoil state:
    // recoilOffsetX is additive camera pitch offset from shooting/dash effects,
    // recoilVelocityX is required for smoothing (SmoothDamp) to return camera to baseline after recoil
    private float recoilOffsetX;
    private float recoilVelocityX;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        dashing = GetComponent<Dashing>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // early out when movement is globally disabled (pausing mid dash, dashing it cutscene future-proofing, etc)
        if (!canMove)
            return;

        // while dashing, movement comes from the dash coroutine only. prevents player moving during dash
        if (dashing == null || !dashing.IsDashing)
            HandleMovement();

        HandleGravity();

        // look and camera feedback are always updated each frame
        HandleLook();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction != null && moveAction.action != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        Vector3 move = (transform.forward * moveInput.y + transform.right * moveInput.x) * walkSpeed;
        characterController.Move(move * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
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

    public void AddPitchOffset(float degrees)
    {
        recoilOffsetX += degrees;
    }
}