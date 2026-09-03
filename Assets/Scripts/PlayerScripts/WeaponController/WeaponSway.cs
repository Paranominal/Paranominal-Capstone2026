using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private PlayerInputReader playerInputReader;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;

    // sway settings control how strongly and how quickly the weapon reacts to mouse look input
    [Header("Sway Settings")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float smooth;
    [SerializeField] private float swayMultiplier;

    [Header("Sway Limits")]
    [SerializeField] private float maxPositionOffset = 0.08f;
    [SerializeField] private float maxRotationAngle = 10f;

    // bob settings define the up/down + side motion while moving.
    // Frequency controls speed of the cycle and amplitude controls how far it moves
    // smoothing controls how quickly the current position catches the target bob position.
    [Header("Walk Bob Settings")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobAmplitude = 0.02f;
    [SerializeField] private float bobSmoothing = 8f;

    // directional movement settings add translation and tilt based on WASD input
    // gives directional weight to strafing/forward movement so motion reads better than just vertical bobbing
    [Header("Movement Direction Settings")]
    [SerializeField] private float movementPositionX = 0.025f;
    [SerializeField] private float movementPositionZ = 0.02f;
    [SerializeField] private float movementTiltX = 2f;
    [SerializeField] private float movementTiltZ = 3f;

    // cached starting local position so all procedural motion is applied as an offset from a stable baseline, preventing drifting
    private Vector3 initialLocalPosition;


    void Awake()
    {
        // record the original local position once so every frame works from this origin point
        initialLocalPosition = transform.localPosition;
    }

    // Update runs every frame and composes the three effects: mouse-driven rotational sway, input-driven movement tilt/offset, bobbing while moving.
    // These are blended smoothly to keep first-person weapon motion readable
    void Update()
    {
        //enableSway = playerInputReader.canMove;
        if (enableSway)
        {
            // read look input from the Input System action and scale to serialized input
            Vector2 lookInput = lookAction != null && lookAction.action != null
                ? lookAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            float mouseX = lookInput.x * swayMultiplier;
            float mouseY = lookInput.y * swayMultiplier;

            // convert mouse movement into pitch/yaw rotation
            // vertical mouse input is inverted so looking up and down makes weapon tilt in intuitive direction
            Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

            // read movement input to drive directional offsets and to scale bob by movement amount
            // magnitude is clamped so diagonal input does not exceed intended max bob strength.
            Vector2 moveInput = moveAction != null && moveAction.action != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            float horizontal = moveInput.x;
            float vertical = moveInput.y;
            float moveAmount = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);

            // positional shift based on movement direction (strafe and forward/back motion)
            // rotational tilt goes in the same direction for a stronger sensation of momentum
            Vector3 movementOffset = new Vector3(horizontal * movementPositionX, 0f, vertical * movementPositionZ);
            Quaternion movementRotation = Quaternion.Euler(-vertical * movementTiltX, 0f, -horizontal * movementTiltZ);

            // Final rotation combines look sway and movement tilt
            Quaternion targetRotation = rotationX * rotationY * movementRotation;
            float rotationAngle = Quaternion.Angle(Quaternion.identity, targetRotation);
            if (rotationAngle > maxRotationAngle)
            {
                targetRotation = Quaternion.RotateTowards(Quaternion.identity, targetRotation, maxRotationAngle);
            }

            // Bob is procedural movement: Y uses sine for the primary bounce rhythm and X uses lower-frequency to add less intense side movement
            // Both are multiplied by moveAmount so bob naturally fades to zero when stationary
            float bobY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude * moveAmount;
            float bobX = Mathf.Cos(Time.time * bobFrequency * 0.5f) * (bobAmplitude * 0.5f) * moveAmount;

            // set desired local position from baseline + bob + directional offset
            Vector3 targetLocalPosition = initialLocalPosition + new Vector3(bobX, bobY, 0f) + movementOffset;
            Vector3 clampedOffset = Vector3.ClampMagnitude(targetLocalPosition - initialLocalPosition, maxPositionOffset);
            targetLocalPosition = initialLocalPosition + clampedOffset;

            // smoothly interpolate toward target transforms to avoid jitter and abrupt snapping
            // Slerp is used for rotation (better for orientations), Lerp for position
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, bobSmoothing * Time.deltaTime);
        }
    }

    public void SetSwayMultiplier(float multiplier)
    {
        swayMultiplier = multiplier;
    }

    public void ToggleSway(bool toggle)
    {
        enableSway = toggle;
    }
}