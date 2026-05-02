using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyVisionSensor))]
public class HobgoblinBehaviour : EnemyBehaviourBase
{
    private enum EnemyState { Roam, Chase }

    //stores the vision sensor and nav agent references
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;

    //patrol path settings
    [Header("Patrolling")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float idleWaitTime = 2f;

    //follow speed settings
    [Header("Following")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float stopDistance = 1f;

    [Header("Status")]
    [SerializeField] private EnemyStagger stagger;

    //current ai state
    private EnemyState currentState = EnemyState.Roam;
    private Vector3 anchorPoint;
    private bool isWaiting;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        anchorPoint = transform.position;
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (stagger == null) stagger = GetComponent<EnemyStagger>();
    }

    //update ai every frame
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

        //decide whether to roam or chase
        UpdateBehaviourState();
        //run the active state
        RunCurrentState();
    }

    //call the active state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Roam:
                PerformRoam();
                break;

            case EnemyState.Chase:
                PerformChase();
                break;
        }
    }

    //use sensor detection to choose the state
    private void UpdateBehaviourState()
    {
        if (IsPlayerDetected())
        {
            currentState = EnemyState.Chase;

            return;
        }

        currentState = EnemyState.Roam;
    }

    //follow the patrol path on the navmesh
    private void PerformRoam()
    {
        if (isWaiting || navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.speed = patrolSpeed;

        if (!navAgent.hasPath || navAgent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtPatrolPoint());
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
        else if (Vector3.Distance(transform.position, VisionTarget.position) <= stopDistance)
        {
            
            navAgent.SetDestination(transform.position);
        }
    }

    //pause before picking a new patrol point
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(idleWaitTime);
        
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        Vector3 nextPoint = anchorPoint + new Vector3(rand.x, 0, rand.y);
        
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.SetDestination(nextPoint);
        
        isWaiting = false;
    }

    //check if the player is in sight or range
    private bool IsPlayerDetected()
    {
        return SensorDetectsTarget();
    }
}