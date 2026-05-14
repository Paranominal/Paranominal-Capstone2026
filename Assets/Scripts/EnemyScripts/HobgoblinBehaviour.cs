using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyVisionSensor))]
public class HobgoblinBehaviour : EnemyBehaviourBase
{
    private enum EnemyState { Chase }

    //stores the vision sensor and nav agent references
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;

    //follow speed settings
    [Header("Following")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float stopDistance = 1f;

    [Header("Status")]
    [SerializeField] private EnemyStagger stagger;

    //current ai state
    private EnemyState currentState = EnemyState.Chase;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (stagger == null) stagger = GetComponent<EnemyStagger>();
    }

    private void Update()
    {
        if (stagger != null && stagger.IsStaggered)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
            return;
        }

        if (!HasVisionTarget)
        {
            return;
        }

        //run the active state
        RunCurrentState();
    }

    //call the active state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Chase:
                PerformChase();
                break;
        }
    }

    //push the agent toward the player
    private void PerformChase()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.isStopped = false;
        navAgent.speed = followSpeed;
        if (VisionTarget != null && Vector3.Distance(transform.position, VisionTarget.position) > stopDistance)
        {
            navAgent.SetDestination(VisionTarget.position);
        }
        else if (VisionTarget != null && Vector3.Distance(transform.position, VisionTarget.position) <= stopDistance)
        {
            navAgent.SetDestination(transform.position);
        }
    }
}