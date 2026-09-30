using UnityEngine;
using UnityEngine.InputSystem;

public class ALTScan : MonoBehaviour
{
    public LayerMask scannable;
    [SerializeField] private float interactRange = 4f; // defines the max interaction distance
    InputAction scanAction;
    InputAction collectAction;  // i started implementing collecting as a seperate set of scripts and then realised it was going to either be so wrapped up in this as to be problematic or duplicate so much code it would be incredibly questionable. so. voila.
    private ALTGrimoire grimoire;
    private ALTScannableObject scannedNow;
    private Raycaster raycaster;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO itemPickupSound;
    [SerializeField] private SoundDataSO scanSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
        collectAction = InputSystem.actions.FindAction("Collect");
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
        if (raycaster == null)
        {
            raycaster = FindAnyObjectByType<Raycaster>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, scannable))
        {
            // Debug.DrawLine(transform.position, hit.point, Color.cyan, 10); // can view this in gizmos mode to help with debugging
            ALTScannableObject tempScan = hit.collider.GetComponent<ALTScannableObject>();

            //triggers stagger when scanned
            EnemyStagger scannedEnemy = hit.collider.GetComponentInParent<EnemyStagger>();


            //Debug.Log("Hit " + tempScan);
            if (scannedNow != tempScan && scannedNow != null)
            {
                scannedNow.outline.enabled = false;
            }
            scannedNow = tempScan;
            scannedNow.outline.enabled = true;
            if (scanAction.WasReleasedThisFrame())
            {
                //for staggering the enemy
                // if (scannedEnemy != null)
                // {
                //     scannedEnemy.OnEnemyScanned();
                // }

                if (scannedNow != null)
                {
                    //Debug.Log(scannedNow.entry);
                    grimoire.AddEntry(scannedNow.entry);
                }
            }
            if (scannedNow != null && collectAction.WasReleasedThisFrame() && scannedNow.collectable)
            {
                // item gets collected here
                if (!grimoire.CompareEntry(scannedNow.entry))   //checks if the entry for the collected item has been scanned and adds it if not
                {
                    grimoire.AddEntry(scannedNow.entry, true);
                    AudioManager.PlaySound(scanSound);
                }
                else
                {
                    grimoire.CollectEntry(scannedNow.entry);
                }

                Debug.Log("Destroyed " + scannedNow.gameObject);
                Destroy(scannedNow.gameObject);
                AudioManager.PlaySound(itemPickupSound);
            }
        }
        else if (scannedNow != null)
        {
            scannedNow.outline.enabled = false;
        }
    }
}
