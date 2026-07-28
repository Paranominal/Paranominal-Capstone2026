using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Centralized raycast from the camera through the pointer.
// Runs early so ScanController and InteractionFocusController can read cached results
// instead of performing their own raycasts each frame.
// EDIT (raycaster-consolidation): now performs a Physics.Raycast and caches the hit.
[DefaultExecutionOrder(-100)]
public class Raycaster : MonoBehaviour
{
    static Raycaster instance;
    public static Raycaster Instance => instance;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask raycastMask;
    [SerializeField] private float maxRange = 20f;

    private Ray ray;
    private RaycastHit hit;
    private bool hasHit;

    public Ray Ray => ray;
    public RaycastHit Hit => hit;
    public bool HasHit => hasHit;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogError("Multiple Raycasters found in the scene.");
        }
    }

    void Update()
    {
        if (Camera.main == null) return;
        if (Pointer.current == null) return;

        ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
        hasHit = Physics.Raycast(ray, out hit, maxRange, raycastMask);
    }
}
