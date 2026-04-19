using UnityEngine;
using UnityEngine.InputSystem;

public class ALTScan : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    InputAction collectAction;  // i started implementing collecting as a seperate set of scripts and then realised it was going to either be so wrapped up in this as to be problematic or duplicate so much code it would be incredibly questionable. so. voila.
    private ALTGrimoire grimoire;
    private ALTScannableObject scannedNow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
        collectAction = InputSystem.actions.FindAction("Collect");
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, scannable))
        {
            // Debug.DrawLine(transform.position, hit.point, Color.cyan, 10); // can view this in gizmos mode to help with debugging
            ALTScannableObject tempScan = hit.collider.GetComponent<ALTScannableObject>();
            //Debug.Log("Hit " + tempScan);
            if (scannedNow != tempScan && scannedNow != null)
            {
                scannedNow.outline.enabled = false;
            }
            scannedNow = tempScan;
            scannedNow.outline.enabled = true;
            if (scanAction.WasReleasedThisFrame())
            {
                //Debug.Log(scannedNow.entry);
                grimoire.AddEntry(scannedNow.entry);
            }
            if (collectAction.WasReleasedThisFrame() && scannedNow.collectable)
            {
                // item gets collected here
                if (!grimoire.CompareEntry(scannedNow.entry))   //checks if the entry for the collected item has been scanned and adds it if not
                {
                    grimoire.AddEntry(scannedNow.entry, true);
                }
                else
                {
                    grimoire.CollectEntry(scannedNow.entry);
                }
                Debug.Log("Destroyed " + scannedNow.gameObject);
                Destroy(scannedNow.gameObject);
                
            }
        }
        else if (scannedNow != null)
        {
            scannedNow.outline.enabled = false;
        }
    }
}
