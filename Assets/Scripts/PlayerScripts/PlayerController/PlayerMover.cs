using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;

    public bool canMove = true;
    public bool CanMove => canMove;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (!canMove)
            return;

        Vector2 moveInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;
        Vector3 move = (transform.forward * moveInput.y + transform.right * moveInput.x) * walkSpeed;
        characterController.Move(move * Time.deltaTime);
    }
}
