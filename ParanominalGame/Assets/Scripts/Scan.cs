using UnityEngine;

public class PlayerScan : MonoBehaviour
{
    public LayerMask GrimoireScan;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Scan();
    }

    void Scan()
    {
        if (Input.GetKeyDown(KeyCode.F))
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
}
