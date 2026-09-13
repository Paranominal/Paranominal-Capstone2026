using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintStrength = 5f;
    [SerializeField] private float slowWalkPercent = 0.3f;

    [Tooltip("Jump height in meters.")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Dash")]
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

    [Header("Inertia")]
    [Tooltip("Time value (seconds) that controls how quickly velocity changes. Larger = more inertia (slower accel and deccel).")]
    [SerializeField] private float inertiaPower = 0.1f;
    [Tooltip("Multiplier applied to inertia when airborne. >1 = more inertia (slower accel/decel) in air.")]
    [SerializeField] private float airInertiaMultiplier = 1.5f;


    [Header("Gravity")]
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float groundedStickForce = -2f;

    

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO playerFootstep;
    [SerializeField] private float footstepInterval = 0.45f;
    private float footstepTimer;

    private CharacterController characterController;

    // velocity smoothing variables
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity;

    // Dash state control variables
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;
    private bool dashHeldLastFrame = false;
    private float dashCooldownTimer = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        // If the input reader is not set or cannot move, skip processing movement
        if (!inputReader.canMove)
            return;

        // Read input values
        Vector2 moveInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;
        bool sprintInput = inputReader != null ? inputReader.SprintInput : false;
        bool slowWalkInput = inputReader != null ? inputReader.SlowWalkInput : false;
        bool jumpInput = inputReader != null ? inputReader.jumpInput : false;
        bool dashInput = inputReader != null ? inputReader.dashInput : false;

        // Calculate desired direction and speed
        Vector3 desiredDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        float desiredSpeed = walkSpeed;

        // handle slow and sprint modifiers
        if (slowWalkInput)
            desiredSpeed *= slowWalkPercent;
        else if (sprintInput)
            desiredSpeed *= sprintStrength;

        // Apply smoothing factor from inertiaPower, a larger inertiaPower means slower acceleration and deceleration.
        // When airborne, scale the time constant so inertia is stronger (slower accel/decel) in air. This allows the player to use the dash-jump move tech to travel farther
        float appliedInertia = inertiaPower * (characterController.isGrounded ? 1f : airInertiaMultiplier);
        float timeConstant = Mathf.Max(0.0001f, appliedInertia);
        float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / timeConstant);
        // Apply smoothing toward desired velocity (same inertia for accel and decel)
        if (!isDashing)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 desiredVelocity = desiredDirection.normalized * desiredSpeed;
                currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, smoothFactor);
            }
            else
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, smoothFactor);
            }
        }

        // Tick dash cooldown timer
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Handle dash start (rising edge: pressed this frame)
        if (dashInput && !dashHeldLastFrame && !isDashing && dashCooldownTimer <= 0f)
        {
            // Only allow starting a dash when grounded unless air dashing is enabled
            if (!characterController.isGrounded && !allowAirDash)
            {
                // cannot start dash in air
                goto SKIP_DASH;
            }

            // if player is giving movement input use that for desired direction, otherwise send them forward
            if (desiredDirection.sqrMagnitude > 0.01f)
                dashDirection = desiredDirection.normalized;
            else
                dashDirection = transform.forward;

            isDashing = true;
            dashTimer = dashDuration;

            // lock horizontal velocity to dash direction
            currentVelocity = dashDirection * dashSpeed;
        }
        SKIP_DASH: ;

        // Handle jump input. If allowDashJump is enabled, jumping while dashing is allowed and will end the dash.
        if (jumpInput && characterController.isGrounded)
        {
            if (!isDashing || allowDashJump)
            {
                // v = sqrt(2 * g * h)
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

                if (isDashing)
                {
                    // end dash early and start cooldown
                    isDashing = false;
                    dashCooldownTimer = dashCooldown;
                }
            }
        }

        // Apply gravity/ground stick
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Update dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                // start cooldown
                dashCooldownTimer = dashCooldown;
                // after dash, keep currentVelocity as whatever horizontal component remains
                // we leave currentVelocity as-is so inertia smoothing will take over
            }
        }

        // When dashing, force horizontal movement along dashDirection at dashSpeed
        Vector3 horizontal = isDashing ? dashDirection * dashSpeed : currentVelocity;

        // Combine horizontal and vertical movement
        Vector3 move = horizontal + Vector3.up * verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        // Footsteps only when not dashing
        if (!isDashing)
            HandleFootsteps(moveInput);

        // update dash input edge detection
        dashHeldLastFrame = dashInput;
    }

    // Plays a footstep when the player is actively moving on the ground, on a fixed interval.
    private void HandleFootsteps(Vector2 moveInput)
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool isGrounded = characterController.isGrounded;

        if (!isMoving || !isGrounded)
        {
            // Reset so the next movement start plays a step immediately
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            if (playerFootstep != null && audioSource != null)
                AudioManager.PlaySound(playerFootstep, audioSource);

            footstepTimer = footstepInterval;
        }
    }

}
