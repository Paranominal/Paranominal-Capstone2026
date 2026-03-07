using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    public LayerMask WeakSpotLayer;
    private InputAction shootAction;
    [SerializeField] private placeholderMuzzleflash muzzleFlash;


    void Awake()
    {
        GetInput();
    }

    void Update()
    {
        if (shootAction.WasPressedThisFrame()) Shoot();
    }

    void Shoot()
    {
        muzzleFlash.MuzzleFlash();

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

    void GetInput()
    {
        shootAction = InputSystem.actions.FindAction("Shoot");
    }
}
