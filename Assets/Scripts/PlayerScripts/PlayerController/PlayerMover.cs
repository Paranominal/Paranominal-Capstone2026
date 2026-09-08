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

    [Header("Inertia")]
    [Tooltip("Time value (seconds) that controls how quickly velocity changes. Larger = more inertia (slower accel and deccel).")]
    [SerializeField] private float inertiaPower = 0.1f;


    [Header("Gravity")]
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO playerFootstep;
    [SerializeField] private float footstepInterval = 0.45f;
    private float footstepTimer;

    private CharacterController characterController;
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (!inputReader.canMove)
            return;

        Vector2 moveInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;
        bool sprintInput = inputReader != null ? inputReader.SprintInput : false;
        bool slowWalkInput = inputReader != null ? inputReader.SlowWalkInput : false;
        bool jumpInput = inputReader != null ? inputReader.jumpInput : false;

        // Calculate desired direction and speed
        Vector3 desiredDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        float desiredSpeed = walkSpeed;

        if (slowWalkInput)
            desiredSpeed *= slowWalkPercent;
        else if (sprintInput)
            desiredSpeed *= sprintStrength;

        // Compute smoothing factor from inertiaPower (time constant). Larger inertiaPower => slower changes.
        float timeConstant = Mathf.Max(0.0001f, inertiaPower);
        float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / timeConstant);

        // Apply smoothing toward desired velocity (same inertia for accel and decel)
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 desiredVelocity = desiredDirection.normalized * desiredSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, smoothFactor);
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, smoothFactor);
        }

        // Handle jump input
        if (jumpInput && characterController.isGrounded)
        {
            // v = sqrt(2 * g * h)
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
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

        // Combine horizontal and vertical movement
        Vector3 move = currentVelocity + Vector3.up * verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        // Footsteps
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
