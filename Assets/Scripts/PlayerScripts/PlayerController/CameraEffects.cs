using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CharacterController characterController;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobFrequency = 12f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobResetSpeed = 5f;

    [Header("Strafe Tilt")]
    [SerializeField] private bool enableStrafeTilt = true;
    [SerializeField] private float maxTiltAngle = 2.5f;
    [SerializeField] private float tiltSpeed = 5f;

    private float bobTimer;
    private Vector3 initialCameraPosition;
    private float currentTilt;

    private void Awake()
    {
        // Try to automatically find references if they are not assigned in the inspector
        if (playerCamera == null) playerCamera = Camera.main;
        if (inputReader == null) inputReader = GetComponentInParent<PlayerInputReader>();
        if (characterController == null) characterController = GetComponentInParent<CharacterController>();
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            initialCameraPosition = playerCamera.transform.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (playerCamera == null) return;
        if (!inputReader.canMove) return;

        UpdateHeadBob();
        UpdateStrafeTilt();
    }

    private void UpdateHeadBob()
    {
        if (!enableHeadBob || characterController == null || inputReader == null) return;

        // Use input magnitude as speed since characterController.velocity gets overwritten by multiple Move calls
        float speed = inputReader.MoveInput.magnitude;

        Vector3 targetPosition = initialCameraPosition;

        if (speed > 0.1f && characterController.isGrounded)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            targetPosition.y += Mathf.Sin(bobTimer) * bobAmplitude;
        }
        else
        {
            // Keep bobTimer where it was, but let targetPosition be initialCameraPosition
            // meaning it will smoothly lerp back to the center when stopped.
            bobTimer = 0f;
        }

        // Smoothly move the camera to the target bob position
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetPosition, Time.deltaTime * bobResetSpeed);
    }

    private void UpdateStrafeTilt()
    {
        if (!enableStrafeTilt || inputReader == null) return;

        float targetTilt = 0f;

        if (characterController != null && characterController.isGrounded)
        {
            float strafeInput = inputReader.MoveInput.x;

            // Negative tilt for right movement, positive for left.
            targetTilt = -strafeInput * maxTiltAngle;
        }

        // Smooth tilt value
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // Apply roll (Z axis) on top of the existing pitch and yaw set by PlayerLook.cs
        Vector3 currentEuler = playerCamera.transform.localRotation.eulerAngles;
        currentEuler.z = currentTilt;

        playerCamera.transform.localRotation = Quaternion.Euler(currentEuler);
    }

    public void ToggleCameraEffects(bool toggle)
    {
        enableHeadBob = toggle;
        enableStrafeTilt = toggle;
    }
    
}
