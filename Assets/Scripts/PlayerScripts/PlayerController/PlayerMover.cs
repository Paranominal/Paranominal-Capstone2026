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

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO playerFootstep;
    [SerializeField] private float footstepInterval = 0.45f;
    private float footstepTimer;

    private CharacterController characterController;

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

        Vector3 move = (transform.forward * moveInput.y + transform.right * moveInput.x) * walkSpeed;
        if (slowWalkInput) move *= slowWalkPercent;
        else if (sprintInput) move *= sprintStrength;
        characterController.Move(move * Time.deltaTime);
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
