using UnityEngine;
using UnityEngine.InputSystem;

public class ALTScan : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    private ALTGrimoire grimoire;
    private ALTScannableObject scannedNow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Grimoire");
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
        }
        else if (scannedNow != null)
        {
            scannedNow.outline.enabled = false;
        }
    }
}
