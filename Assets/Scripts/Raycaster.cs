using UnityEngine;

// Michael feature (raycaster-upgrade): added Instance accessor, HasHit, Hit properties.
public class Raycaster : MonoBehaviour
{
    [Tooltip("Maximum raycast distance.")]
    [SerializeField] private float maxRange = 100f;
    [Tooltip("Layers the raycast can hit.")]
    [SerializeField] private LayerMask raycastMask = ~0;

    private static Raycaster _instance;
    public static Raycaster Instance => _instance;

    private Ray ray;
    private RaycastHit hit;
    private bool hasHit;

    public Ray Ray => ray;
    public RaycastHit Hit => hit;
    public bool HasHit => hasHit;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Debug.LogError("Multiple Raycasters found in the scene.");
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            hasHit = false;
            return;
        }

        ray = new Ray(cam.transform.position, cam.transform.forward);
        hasHit = Physics.Raycast(ray, out hit, maxRange, raycastMask);
    }
}
