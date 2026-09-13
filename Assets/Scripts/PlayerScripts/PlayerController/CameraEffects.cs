using UnityEngine;

// Michael feature (camera-shake): integrated Perlin noise based camera shake. Applied after head bob and strafe tilt so all three effects layer cleanly.
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

    [Header("Camera Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float defaultShakeIntensity = 0.3f;
    [SerializeField] private float defaultShakeDuration = 0.25f;
    [Tooltip("How much positional offset (X/Y) to apply at full intensity.")]
    [SerializeField] private float shakePositionScale = 0.08f;
    [Tooltip("How much Z roll (degrees) to apply at full intensity.")]
    [SerializeField] private float shakeRollScale = 2f;
    [Tooltip("Perlin noise sample speed. Higher = faster wobble.")]
    [SerializeField] private float shakeFrequency = 25f;

    private float bobTimer;
    private Vector3 initialCameraPosition;
    private float currentTilt;

    // Shake state
    private float shakeIntensity;
    private float shakeDuration;
    private float shakeElapsed;
    private float seedX;
    private float seedY;
    private float seedR;
    private bool isShaking;

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
        UpdateShake();
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

    // Summary: Applies Perlin noise shake additively on top of head bob and strafe tilt.
    private void UpdateShake()
    {
        if (!enableShake)
        {
            // If shake was disabled while active, stop applying effects and reset state
            isShaking = false;
            return;
        }

        if (!isShaking) return;

        shakeElapsed += Time.deltaTime;

        if (shakeElapsed >= shakeDuration)
        {
            isShaking = false;
            return;
        }

        float t = shakeElapsed / shakeDuration;
        float decay = 1f - t * t;
        float scale = shakeIntensity * decay;

        float time = shakeElapsed * shakeFrequency;

        float offsetX = (Mathf.PerlinNoise(seedX + time, 0f) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(seedY + time, 0f) - 0.5f) * 2f;
        float roll    = (Mathf.PerlinNoise(seedR + time, 0f) - 0.5f) * 2f;

        // layer on top of whatever head bob set.
        playerCamera.transform.localPosition += new Vector3(
            offsetX * shakePositionScale * scale,
            offsetY * shakePositionScale * scale,
            0f);

        // layer roll on top of whatever strafe tilt set.
        Vector3 euler = playerCamera.transform.localRotation.eulerAngles;
        euler.z += roll * shakeRollScale * scale;
        playerCamera.transform.localRotation = Quaternion.Euler(euler);
    }

    // Start a shake with default intensity and duration. Restarts if already shaking.
    public void Shake()
    {
        if (!enableShake) return;
        Shake(defaultShakeIntensity, defaultShakeDuration);
    }

    // Start a shake with custom intensity and duration. Restarts if already shaking.
    public void Shake(float intensity, float duration)
    {
        if (!enableShake) return;
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeElapsed = 0f;
        isShaking = true;

        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        seedR = Random.Range(0f, 1000f);
    }

    public void ToggleCameraEffects(bool toggle)
    {
        enableHeadBob = toggle;
        enableStrafeTilt = toggle;
        enableShake = toggle;
    }

    // Expose runtime control for shake independently
    public void ToggleCameraShake(bool enable)
    {
        enableShake = enable;
        if (!enable)
        {
            // stop any active shake immediately
            isShaking = false;
        }
    }
    
}
