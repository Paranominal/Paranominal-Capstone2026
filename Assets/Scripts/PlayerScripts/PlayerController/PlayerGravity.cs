using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerGravity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader playerInputReader;
    [SerializeField] private PlayerMover playerMover;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();
    }

    private void Update()
    {
        if (playerMover != null && !playerInputReader.CanMove)
            return;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
