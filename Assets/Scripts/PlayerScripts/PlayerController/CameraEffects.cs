using UnityEngine;

// Michael feature (camera-shake): integrated Perlin noise based camera shake. Applied after head bob and strafe tilt so all three effects layer cleanly.
[DefaultExecutionOrder(100)]
public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance { get; private set; }

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

    // Michael feature (special-shot): held shake is separate from timed impact shake.
    private float sustainedShakeIntensity;
    private float noiseTime;
    private Quaternion lastShakeRotation;
    private Quaternion lastBaseRotation;
    private bool appliedShakeRotation;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        seedR = Random.Range(0f, 1000f);

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

        UpdateShake(initialCameraPosition, GetBaseRotation());
    }

    // Remove our previous roll only if another camera controller has not replaced the rotation.
    private Quaternion GetBaseRotation()
    {
        Quaternion rotation = playerCamera.transform.localRotation;
        if (appliedShakeRotation && Quaternion.Angle(rotation, lastShakeRotation) < 0.001f)
            rotation = lastBaseRotation;
        appliedShakeRotation = false;
        return rotation;
    }

    // Summary: Layers sustained charge shake and fading impact shake without accumulating roll.
    private void UpdateShake(Vector3 basePosition, Quaternion baseRotation)
    {
        float scale = 0f;
        if (enableShake)
        {
            scale = sustainedShakeIntensity;
            if (isShaking)
            {
                shakeElapsed += Time.deltaTime;
                if (shakeDuration <= 0f || shakeElapsed >= shakeDuration)
                    isShaking = false;
                else
                {
                    float t = shakeElapsed / shakeDuration;
                    scale += shakeIntensity * (1f - t * t);
                }
            }
        }
        else
        {
            isShaking = false;
            sustainedShakeIntensity = 0f;
        }

        playerCamera.transform.localPosition = basePosition;
        playerCamera.transform.localRotation = baseRotation;
        if (scale <= 0f) return;

        noiseTime += Time.deltaTime * shakeFrequency;
        float offsetX = (Mathf.PerlinNoise(seedX + noiseTime, 0f) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(seedY + noiseTime, 0f) - 0.5f) * 2f;
        float roll = (Mathf.PerlinNoise(seedR + noiseTime, 0f) - 0.5f) * 2f;

        playerCamera.transform.localPosition = basePosition +
            new Vector3(offsetX, offsetY, 0f) * shakePositionScale * scale;
        lastBaseRotation = baseRotation;
        lastShakeRotation = baseRotation * Quaternion.Euler(0f, 0f, roll * shakeRollScale * scale);
        playerCamera.transform.localRotation = lastShakeRotation;
        appliedShakeRotation = true;
    }

    // Michael feature (special-shot): update intensity without restarting the noise each frame.
    public void SetSustainedShake(float intensity)
    {
        sustainedShakeIntensity = enableShake ? Mathf.Max(0f, intensity) : 0f;
    }

    public void StopSustainedShake()
    {
        sustainedShakeIntensity = 0f;
    }

    private void OnDisable()
    {
        isShaking = false;
        sustainedShakeIntensity = 0f;
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = GetBaseRotation();
            playerCamera.transform.localPosition = initialCameraPosition;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Start a shake with default intensity and duration. Restarts if already shaking.
    public void Shake()
    {
        if (!enableShake) return;
        Shake(defaultShakeIntensity, defaultShakeDuration);
    }

    public void Shake(float intensity)
    {
        if (!enableShake) return;
        Shake(intensity, defaultShakeDuration);
    }

    // Start a shake with custom intensity and duration. Restarts if already shaking.
    public void Shake(float intensity, float duration)
    {
        if (!enableShake) return;
        shakeIntensity = Mathf.Max(0f, intensity);
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
            sustainedShakeIntensity = 0f;
        }
    }
    
}
