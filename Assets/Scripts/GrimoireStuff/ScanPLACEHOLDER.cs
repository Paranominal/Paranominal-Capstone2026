using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScan : MonoBehaviour
{
    public LayerMask GrimoireScan;
    private InputAction scanAction;
    void Awake()
    {
        GetInput();
    }

    void Update()
    {
        Scan();
    }

    void Scan()
    {
        if (scanAction.WasPressedThisFrame())
        {
            //raycast
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, GrimoireScan))
            {
                hit.collider.GetComponent<ScannableObject>().OnScan();
                Debug.Log("Scanned Weakpoint! " + hit.collider.name);
            }
            else
            {
                Debug.Log("Scanned Nothing...");
            }
        }
    }
    void GetInput()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
    }
}
