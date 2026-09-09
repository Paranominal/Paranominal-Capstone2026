// Summary:
// Ground enemy behaviour using NavMeshAgent for movement.
// Extends EnemyBehaviourBase with NavMesh-specific chase, retreat, strafe, and return.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviour : EnemyBehaviourBase
{
    [Header("NavMesh Movement")]
    [SerializeField] private NavMeshAgent navAgent;
    [Range(0, 1)]
    [Tooltip("Controls how quickly the agent accelerates. 0 = sluggish, 1 = snappy.")]
    [SerializeField] private float chaseEasing = 0.5f;

    protected override void InitializeMovement()
    {
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.acceleration = 51f - chaseEasing * 50f;
            navAgent.updateRotation = false;
        }
    }

    protected override void DoMove(Vector3 target, float speed, float stopDistance)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        if (navAgent.isStopped) navAgent.isStopped = false;
        navAgent.stoppingDistance = stopDistance;
        navAgent.speed = speed;
        navAgent.SetDestination(target);
    }

    protected override void DoStop()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    protected override bool HasReachedTarget()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return true;
        if (navAgent.pathPending) return false;
        return !navAgent.hasPath || navAgent.remainingDistance <= navAgent.stoppingDistance + 0.1f;
    }

    // check if the path to the strafe target crosses a NavMesh edge (wall)
    protected override bool IsStrafeClear(Vector3 target)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return true;
        return !NavMesh.Raycast(transform.position, target, out NavMeshHit _, navAgent.areaMask);
    }

    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        if (isPaused)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
        else
        {
            navAgent.isStopped = false;
        }
    }
}