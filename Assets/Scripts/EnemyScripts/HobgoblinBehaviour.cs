using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class HobgoblinBehaviour : MonoBehaviour
{
    private enum EnemyState { Roam, Chase }

    //stores the vision sensor and nav agent references
    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;
    [SerializeField] private NavMeshAgent navAgent;

    //keeps chase memory after losing sight
    [Header("Detection")]
    [SerializeField] private float loseSightMemory = 2.0f;    // How long to chase after losing sight

    //patrol path settings
    [Header("Patrolling")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float idleWaitTime = 2f;

    //chase speed settings
    [Header("Chasing")]
    [SerializeField] private float chaseSpeed = 5f;

    //current ai state
    private EnemyState currentState = EnemyState.Roam;
    private Transform player;
    private Vector3 anchorPoint;
    private float lastSeenTime = float.NegativeInfinity;
    private bool isWaiting;
    private bool isPlayerVisible;

    //cache references before play starts
    private void Awake()
    {
        anchorPoint = transform.position;
        ResolveReferences();
        vision.AcquirePlayerTarget();
        player = vision.Target;
    }

    //update ai every frame
    private void Update()
    {
        if (vision == null)
        {
            return;
        }

        vision.AcquirePlayerTarget();

        if (!vision.HasTarget)
        {
            return;
        }

        player = vision.Target;

        //refresh player visibility
        isPlayerVisible = IsPlayerDetected();

        if (isPlayerVisible)
        {
            lastSeenTime = Time.time;
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

    //return to roaming
    private void EnterRoamState()
    {
        currentState = EnemyState.Roam;
    }

    //switch into chase
    private void EnterChaseState()
    {
        currentState = EnemyState.Chase;
    }

    //use sight memory to choose the state
    private void UpdateBehaviourState()
    {
        bool recentlySeen = (Time.time - lastSeenTime) <= loseSightMemory;

        if (isPlayerVisible || recentlySeen)
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChaseState();
            }

            return;
        }

        if (currentState != EnemyState.Roam)
        {
            EnterRoamState();
        }
    }

    //follow the patrol path on the navmesh
    private void PerformRoam()
    {
        if (isWaiting || navAgent == null) return;

        navAgent.speed = patrolSpeed;

        if (!navAgent.hasPath || navAgent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    //push the agent toward the player
    private void PerformChase()
    {
        if (navAgent == null) return;

        navAgent.isStopped = false;
        navAgent.speed = chaseSpeed;
        if (player != null)
        {
            navAgent.SetDestination(player.position);
        }
    }

    //pause before picking a new patrol point
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(idleWaitTime);
        
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        Vector3 nextPoint = anchorPoint + new Vector3(rand.x, 0, rand.y);
        
        if (navAgent != null) navAgent.SetDestination(nextPoint);
        
        isWaiting = false;
    }

    //check if the player is in sight or range
    private bool IsPlayerDetected()
    {
        if (vision != null)
        {
            return vision.IsTargetDetected();
        }

        return false;
    }

    //find missing sensor or agent components
    private void ResolveReferences()
    {
        if (vision == null) vision = GetComponent<EnemyVisionSensor>();
        if (vision == null) vision = gameObject.AddComponent<EnemyVisionSensor>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
    }
}