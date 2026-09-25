using UnityEngine;
using UnityEngine.InputSystem;
// EDIT (special-shot): needed for the piercing hit list.
using System.Collections.Generic;

public class WeaponHitscan : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Hitscan")]
    [SerializeField] private LayerMask weakPointLayer;
    [SerializeField] private LayerMask ignoreLayer;
    [SerializeField] private float rayDistance = 1000f;

    [Header("Miss Popup Distance")]
    [SerializeField] private float maxMissPopupDistance = 8f;
    [SerializeField] private float minMissPopupDistance = 2f;

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
        LayerMask mask = ~ignoreLayer;
        bool hasHit = Physics.Raycast(ray, out targetHit, rayDistance, mask, QueryTriggerInteraction.Collide);
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
        LayerMask mask = ~ignoreLayer;
        bool hasHit = Physics.Raycast(ray, out damageableHit, rayDistance, mask, QueryTriggerInteraction.Collide);
        if (!hasHit)
            return false;

        
        damageable = damageableHit.collider.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = damageableHit.collider.GetComponentInParent<IDamageable>();

        if (damageableHit.collider.gameObject == GameObject.FindWithTag("Player"))
            return false;

        return damageable != null;
    }

    public Vector3 LogWorldHitOrMiss()
    {
        if (playerCamera == null)
            return Vector3.zero;

        Ray ray = BuildAimRay();
        LayerMask mask = ~ignoreLayer;

        if (Physics.Raycast(ray, out RaycastHit hitAny, rayDistance, mask, QueryTriggerInteraction.Collide))
        {
            Debug.Log("Hit! " + hitAny.collider.name + " at " + hitAny.distance);
            float distance = Mathf.Max(hitAny.distance, minMissPopupDistance);
            return ray.origin + ray.direction * distance;
        }
        else
        {
            Debug.Log("Miss...");
            return ray.origin + ray.direction * maxMissPopupDistance;
        }
            
    }

    // EDIT (special-shot): piercing raycast for the Special Shot.
    // Returns hits on enemies and weakpoints along the aim ray, closest first, stopping at the first solid world collider.
    // Triggers that aren't enemies or weakpoints are passed through. missPoint mirrors LogWorldHitOrMiss for the miss popup.
    public List<RaycastHit> GetSpecialShotHits(out Vector3 missPoint)
    {
        List<RaycastHit> results = new List<RaycastHit>();
        missPoint = Vector3.zero;

        if (playerCamera == null || raycaster == null)
            return results;

        Ray ray = BuildAimRay();
        LayerMask mask = ~ignoreLayer | weakPointLayer;
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, mask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        missPoint = ray.origin + ray.direction * maxMissPopupDistance;

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col.CompareTag("Player"))
                continue;

            bool isTarget = col.GetComponentInParent<WeakPoint>() != null || col.GetComponentInParent<Enemy>() != null;
            if (isTarget)
            {
                results.Add(hit);
                continue;
            }

            // non-target triggers (detectors, zones etc) don't block the shot
            if (col.isTrigger)
                continue;

            // solid world geometry stops the shot
            missPoint = ray.origin + ray.direction * Mathf.Max(hit.distance, minMissPopupDistance);
            break;
        }

        return results;
    }

    public Ray AimRay => raycaster != null ? raycaster.Ray : default;

    private Ray BuildAimRay()
    {
        return raycaster.Ray;
    }
}
