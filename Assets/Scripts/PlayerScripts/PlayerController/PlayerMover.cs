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
    [SerializeField] private PlayerDash playerDash;

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

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();
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
        if (playerDash == null || !playerDash.IsDashing)
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
        // PlayerDash handles dash state and input
        if (playerDash != null)
            playerDash.HandleDashInput(dashInput, desiredDirection, transform, characterController, Time.deltaTime);

        // Handle jump input. If allowDashJump is enabled, jumping while dashing is allowed and will end the dash.
        if (jumpInput && characterController.isGrounded)
        {
            bool dashActive = playerDash != null && playerDash.IsDashing;
            bool allowDashJumpLocal = playerDash != null ? playerDash.AllowDashJump : false;

            if (!dashActive || allowDashJumpLocal)
            {
                // v = sqrt(2 * g * h)
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

                if (dashActive && playerDash != null)
                {
                    // end dash early and start cooldown
                    playerDash.CancelDashAndStartCooldown();
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

        // When dashing, PlayerDash provides the horizontal dash velocity
        Vector3 horizontal = (playerDash != null && playerDash.IsDashing) ? playerDash.CurrentDashVelocity : currentVelocity;

        // Combine horizontal and vertical movement
        Vector3 move = horizontal + Vector3.up * verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        // Footsteps only when not dashing
        if (playerDash == null || !playerDash.IsDashing)
            HandleFootsteps(moveInput);

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
