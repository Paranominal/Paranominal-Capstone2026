// Summary:
// Teleports the enemy to a random valid NavMesh point when stagger ends, if the player is visible. 
// Sits alongside the behaviour system as astandalone component, not a behaviour subclass.

using UnityEngine;
using UnityEngine.AI;

public class HobgoblinTeleport : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStagger stagger;
    [SerializeField] private EnemyVisionSensor vision;

    [Header("Teleport Escape")]
    [SerializeField] private float teleportDistance = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private NavMeshAgent navAgent;
    private Collider roomCollider;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        if (stagger == null) stagger = GetComponent<EnemyStagger>();
        if (vision == null) vision = GetComponent<EnemyVisionSensor>();

        // try to find the room bounds from parent RoomEntryDetector
        RoomEntryDetector roomDetector = GetComponentInParent<RoomEntryDetector>();
        if (roomDetector != null)
            roomCollider = roomDetector.GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (stagger != null) stagger.OnStaggerEnd += HandleStaggerEnded;
    }

    private void OnDisable()
    {
        if (stagger != null) stagger.OnStaggerEnd -= HandleStaggerEnded;
    }

    private void HandleStaggerEnded()
    {
        if (vision != null && vision.HasTarget)
            TeleportToRandomNavMeshPoint();

        if (debugMode) Debug.Log($"[HobgoblinTeleport] Stagger ended for {gameObject.name}", gameObject);
    }

    private void TeleportToRandomNavMeshPoint()
    {
        if (!TryGetRandomTeleportTarget(out Vector3 teleportTarget)) return;

        if (navAgent != null && navAgent.isOnNavMesh)
            navAgent.Warp(teleportTarget);
        else
            transform.position = teleportTarget;

        if (debugMode) Debug.Log($"[HobgoblinTeleport] {gameObject.name} teleported to {teleportTarget}", gameObject);
    }

    private bool TryGetRandomTeleportTarget(out Vector3 teleportTarget)
    {
        teleportTarget = transform.position;

        Vector3 searchCenter = transform.position;
        float searchRadius = teleportDistance * 3f;

        if (roomCollider != null)
        {
            Bounds bounds = roomCollider.bounds;
            searchCenter = new Vector3(bounds.center.x, transform.position.y, bounds.center.z);
            searchRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint;

            if (roomCollider != null)
            {
                Bounds bounds = roomCollider.bounds;
                randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    transform.position.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );
            }
            else
            {
                randomPoint = searchCenter + Random.insideUnitSphere * searchRadius;
                randomPoint.y = transform.position.y;
            }

            if (navAgent != null && navAgent.isOnNavMesh)
            {
                // tiny radius so it doesn't grab NavMesh on the other side of a wall
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 0.5f, navAgent.areaMask))
                {
                    // NavMesh raycast ensures we don't cross a wall boundary
                    if (!NavMesh.Raycast(transform.position, hit.position, out NavMeshHit _, navAgent.areaMask))
                    {
                        teleportTarget = hit.position;
                        return true;
                    }
                }
            }
            else
            {
                // fallback without NavMesh
                if (roomCollider == null || Vector3.Distance(randomPoint, roomCollider.ClosestPoint(randomPoint)) < 0.1f)
                {
                    teleportTarget = randomPoint;
                    return true;
                }
            }
        }

        return false;
    }
}