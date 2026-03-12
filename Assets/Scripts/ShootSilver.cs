using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ShootSilver : MonoBehaviour
{
    public LayerMask WeakSpotLayer;
    private InputAction shootAction;
    [SerializeField] private SpriteRenderer muzzleFlash;
    private bool onCooldown = false;
    [SerializeField] float shotCooldownTimer = 0.1f;


    void Awake()
    {
        muzzleFlash.enabled = false;
        GetInput();
    }

    void Update()
    {
        if (shootAction.WasPressedThisFrame()) Shoot();
    }

    void Shoot()
    {
        if (onCooldown) return;

        StartCoroutine(SilverFlash());

        //raycast shooting
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, WeakSpotLayer))
        {
            hit.collider.GetComponent<WeakPoint>().OnHit(WeakPointType.Silver);
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
        
        StartCoroutine(SilverCooldown());
    }
    public IEnumerator SilverFlash()
    {
        muzzleFlash.GetComponent<SpriteRenderer>().enabled = true;
        yield return new WaitForSeconds(0.2f);
        muzzleFlash.GetComponent<SpriteRenderer>().enabled = false;
    }

    private IEnumerator SilverCooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(shotCooldownTimer);
        onCooldown = false;
    }

    void GetInput()
    {
        shootAction = InputSystem.actions.FindAction("ShootSilver");
    }
}
