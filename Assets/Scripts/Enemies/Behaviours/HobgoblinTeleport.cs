using UnityEngine;
using UnityEngine.AI;

public class HobgoblinTeleport : EnemyBehaviourBase
{
    //references resolved at awake
    private NavMeshAgent navAgent;
    private EnemyStagger stagger;

    [Header("Teleport Escape")]
    //distance to teleport away from player
    [SerializeField] private float teleportDistance = 5f; 
    [SerializeField] private bool debugMode = false;

    private Collider roomCollider;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        navAgent = GetComponent<NavMeshAgent>();
        stagger = GetComponent<EnemyStagger>();

        //try to find the room bounds from parent roomentrydetector
        RoomEntryDetector roomDetector = GetComponentInParent<RoomEntryDetector>();
        if (roomDetector != null)
        {
            roomCollider = roomDetector.GetComponent<Collider>();
        }

        //subscribe to stagger end event
        if (stagger != null)
        {
            stagger.OnStaggerEnd += HandleStaggerEnded;
        }
    }

    //called when stagger ends, triggers teleport if player is visible
    private void HandleStaggerEnded()
    {
        if (HasVisionTarget)
        {
            TeleportToRandomNavMeshPoint();
        }

        if (debugMode)
            Debug.Log($"[HobgoblinTeleport] Stagger ended for {gameObject.name}", gameObject);
    }

    private void TeleportToRandomNavMeshPoint()
    {
        if (!TryGetRandomTeleportTarget(out Vector3 teleportTarget))
        {
            return;
        }

        //move the navmesh agent to the new position
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.Warp(teleportTarget);
        }
        else
        {
            transform.position = teleportTarget;
        }

        if (debugMode)
            Debug.Log($"[HobgoblinTeleport] {gameObject.name} teleported away from player to {teleportTarget}", gameObject);
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
                //use a tiny radius so it never grabs navmesh on the other side of a wall
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 0.5f, navAgent.areaMask))
                {
                    //use navmesh raycast to ensure we do not cross a wall boundary
                    if (!NavMesh.Raycast(transform.position, hit.position, out NavMeshHit navHit, navAgent.areaMask))
                    {
                        teleportTarget = hit.position;
                        return true;
                    }
                }
            }
            else
            {
                //fallback validation check without navmesh
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