using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public LayerMask WeakSpotLayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Shoot();
    }

    void Shoot()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            //raycast
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, WeakSpotLayer))
            {
                hit.collider.GetComponent<WeakPoint>().OnHit();
                Debug.Log("Hit Weakpoint! " + hit.collider.name);
            }
            else if (Physics.Raycast(ray, out hit, 1000f, ~WeakSpotLayer))
            {
                Debug.Log("Hit! " + hit.collider.name);
            }
            else
            {
                Debug.Log("Miss...");
            }
        }
    }
}
