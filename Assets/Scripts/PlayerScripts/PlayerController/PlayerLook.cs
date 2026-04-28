using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private CameraRecoilController cameraRecoil;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float lookXLimit = 60f;
    [SerializeField] private float lookSmoothing = 0.03f;

    private Vector2 smoothedLookDelta;
    private Vector2 lookDeltaVelocity;
    private float cameraPitch;

    private void Awake()
    {
        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();

        if (cameraRecoil == null)
            cameraRecoil = GetComponent<CameraRecoilController>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (playerMover != null && !playerMover.CanMove)
            return;

        Vector2 rawLookInput = inputReader != null ? inputReader.LookInput : Vector2.zero;

        smoothedLookDelta = Vector2.SmoothDamp(
            smoothedLookDelta,
            rawLookInput,
            ref lookDeltaVelocity,
            lookSmoothing
        );

        float mouseX = smoothedLookDelta.x * lookSensitivity;
        float mouseY = smoothedLookDelta.y * lookSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);

        float recoilOffset = cameraRecoil != null ? cameraRecoil.RecoilOffsetX : 0f;

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch + recoilOffset, 0f, 0f);

        transform.Rotate(0f, mouseX, 0f);
    }
}
