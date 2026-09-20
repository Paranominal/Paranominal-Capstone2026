using UnityEngine;

// Michael feature (camera-shake): integrated Perlin noise based camera shake. Applied after head bob and strafe tilt so all three effects layer cleanly.
public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CharacterController characterController;

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
    public float shakeDuration;
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

        
        UpdateShake(initialCameraPosition, playerCamera.transform.localRotation);
    }

    // Summary: Applies Perlin noise shake additively on top of head bob and strafe tilt.
    private void UpdateShake(Vector3 basePosition, Quaternion baseRotation)
    {
        if (!enableShake)
        {
            // If shake was disabled while active, stop applying effects and reset state
            isShaking = false;
            // returns camera to base position and rotation (needed for stun effect)
            playerCamera.transform.localPosition = basePosition;
            playerCamera.transform.localRotation = baseRotation;
            return;
        }

        if (!isShaking)
        {
            // If not shaking, ensure camera is at base position and rotation
            playerCamera.transform.localPosition = basePosition;
            playerCamera.transform.localRotation = baseRotation;
            return;
        }

        shakeElapsed += Time.deltaTime;

        if (shakeElapsed >= shakeDuration)
        {
            isShaking = false;
            playerCamera.transform.localPosition = basePosition;
            playerCamera.transform.localRotation = baseRotation;
            return;
        }

        float t = shakeElapsed / shakeDuration;
        float decay = 1f - t * t;
        float scale = shakeIntensity * decay;
        float time = shakeElapsed * shakeFrequency;

        float offsetX = (Mathf.PerlinNoise(seedX + time, 0f) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(seedY + time, 0f) - 0.5f) * 2f;
        float roll    = (Mathf.PerlinNoise(seedR + time, 0f) - 0.5f) * 2f;

        Vector3 shakeOffset = new Vector3(offsetX * shakePositionScale * scale, offsetY * shakePositionScale * scale, 0f);
        float rollDegrees = roll * shakeRollScale * scale;

        // applies the shake transform explicitly relative to the base position
        playerCamera.transform.localPosition = basePosition + shakeOffset;

        // applies roll on top of the base rotation to preserve players pitch/yaw
        Vector3 baseEuler = baseRotation.eulerAngles;
        baseEuler.z += rollDegrees;
        playerCamera.transform.localRotation = Quaternion.Euler(baseEuler);
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
