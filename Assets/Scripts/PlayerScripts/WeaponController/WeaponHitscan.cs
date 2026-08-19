using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHitscan : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Hitscan")]
    [SerializeField] private LayerMask weakPointLayer;
    [SerializeField] private float rayDistance = 1000f;

    private Raycaster raycaster;

    private void Start()
    {
        if (raycaster == null)
        {
            raycaster = FindAnyObjectByType<Raycaster>();
        }
    }

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    public bool TryGetWeakPointHit(out WeakPoint weakPoint, out RaycastHit weakHit)
    {
        weakPoint = null;

        if (playerCamera == null)
        {
            weakHit = default;
            return false;
        }

        Ray ray = BuildAimRay();
        bool hasWeakHit = Physics.Raycast(ray, out weakHit, rayDistance, weakPointLayer, QueryTriggerInteraction.Collide);
        if (!hasWeakHit)
            return false;

        weakPoint = weakHit.collider.GetComponent<WeakPoint>();
        if (weakPoint == null)
            weakPoint = weakHit.collider.GetComponentInParent<WeakPoint>();

        return weakPoint != null;
    }

    public bool TryGetShootableTargetHit(out ShootableTarget target, out RaycastHit targetHit)
    {
        target = null;

        if (playerCamera == null)
        {
            targetHit = default;
            return false;
        }

        Ray ray = BuildAimRay();
        bool hasHit = Physics.Raycast(ray, out targetHit, rayDistance, ~0, QueryTriggerInteraction.Collide);
        if (!hasHit)
            return false;

        target = targetHit.collider.GetComponent<ShootableTarget>();
        if (target == null)
            target = targetHit.collider.GetComponentInParent<ShootableTarget>();

        return target != null;
    }

    //allows for interactions with idamageable for the enemy states
    public bool TryGetDamageableHit(out IDamageable damageable, out RaycastHit damageableHit)
    {
        damageable = null;

        if (playerCamera == null)
        {
            damageableHit = default;
            return false;
        }

        Ray ray = BuildAimRay();
        
        bool hasHit = Physics.Raycast(ray, out damageableHit, rayDistance, ~0, QueryTriggerInteraction.Collide);
        if (!hasHit)
            return false;

        
        damageable = damageableHit.collider.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = damageableHit.collider.GetComponentInParent<IDamageable>();

        if (damageableHit.collider.gameObject == GameObject.FindWithTag("Player"))
            return false;

        return damageable != null;
    }

    public void LogWorldHitOrMiss()
    {
        if (playerCamera == null)
            return;

        Ray ray = BuildAimRay();

        if (Physics.Raycast(ray, out RaycastHit hitAny, rayDistance, ~0, QueryTriggerInteraction.Collide))
            Debug.Log("Hit! " + hitAny.collider.name);
        else
            Debug.Log("Miss...");
    }

    private Ray BuildAimRay()
    {
        return raycaster.Ray;
    }
}
