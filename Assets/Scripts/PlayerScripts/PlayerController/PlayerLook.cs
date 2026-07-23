using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
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
        if (Time.timeScale == 0f)
            return;

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

        player.transform.Rotate(0f, mouseX, 0f);
    }

    public void SetLookSensitivity(float newSensitivity)
    {
        lookSensitivity = newSensitivity;
    }

    public float GetLookSensitivity()
    {
        return lookSensitivity;
    }
    
    // Looking at
    [Header("Looking At")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private float lookAtRange = 4;
    public GameObject LookingAt()
    {
        Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, lookAtRange, interactableMask);
        if (hit.collider != null && !hit.collider.gameObject.isStatic) return hit.collider.gameObject;
        else return null;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * lookAtRange);
    }
}
