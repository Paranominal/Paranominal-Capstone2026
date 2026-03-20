using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyStateMachine : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Roam,
        Chase,
        Attack
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Roaming Settings")]
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float roamPointReachedDistance = 1f;

    [Header("Idle Settings")]
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 5f;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private float meleeDamage = 5f;
    [SerializeField] private float meleeRange = 10f;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 10f;

    private bool isPlayerVisible;
    private bool isPlayerInRange;

    private EnemyState currentState;

    private Vector3 currentRoamPoint;
    private bool roamPointSet;

    private float idleTimer;
    private float currentIdleDuration;

    // Sets up component references when the object is first loaded.
    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
    }

    // Places the enemy into its starting idle state when the scene begins.
    private void Start()
    {
        EnterIdleState();
    }

    // Updates player detection, state transitions, and current state behaviour each frame.
    private void Update()
    {
        DetectPlayer();
        UpdateBehaviourState();
        RunCurrentState();
    }

    // Draws the enemy's detection ranges and current roaming area in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, roamRadius);

        if (roamPointSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentRoamPoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentRoamPoint);
        }
    }

    // Checks whether the player is visible and within engagement range.
    private void DetectPlayer()
    {
        isPlayerVisible = Physics.CheckSphere(transform.position, visionRange, playerLayerMask);
        isPlayerInRange = Physics.CheckSphere(transform.position, engagementRange, playerLayerMask);
    }

    // Performs the enemy's melee attack behaviour.
    private void MeleeAttack()
    {
        // TO-DO: Implement basic enemy melee attack
    }

    // Finds a valid roaming destination within the enemy's roaming radius.
    private void GenerateRoamPoint()
    {
        roamPointSet = false;

        int attempts = 0;
        int maxAttempts = 10;

        while (!roamPointSet && attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(-roamRadius, roamRadius);
            float randomZ = Random.Range(-roamRadius, roamRadius);

            Vector3 potentialPoint = new Vector3(
                transform.position.x + randomX,
                transform.position.y + 2f,
                transform.position.z + randomZ
            );

            if (Physics.Raycast(potentialPoint, Vector3.down, out RaycastHit hit, 10f, terrainLayer))
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hit.point, out navHit, 2f, NavMesh.AllAreas))
                {
                    currentRoamPoint = navHit.position;
                    roamPointSet = true;
                }
            }
        }
    }

    // Handles the delay between consecutive enemy attacks.
    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    // Switches the enemy into the idle state and sets a random idle duration.
    private void EnterIdleState()
    {
        currentState = EnemyState.Idle;
        navAgent.SetDestination(transform.position);

        idleTimer = 0f;
        currentIdleDuration = Random.Range(idleTimeMin, idleTimeMax);
    }

    // Switches the enemy into the roam state and selects a new roam destination.
    private void EnterRoamState()
    {
        currentState = EnemyState.Roam;
        GenerateRoamPoint();

        if (roamPointSet)
        {
            navAgent.SetDestination(currentRoamPoint);
        }
        else
        {
            EnterIdleState();
        }
    }

    // Switches the enemy into the chase state.
    private void EnterChaseState()
    {
        currentState = EnemyState.Chase;
    }

    // Switches the enemy into the attack state.
    private void EnterAttackState()
    {
        currentState = EnemyState.Attack;
    }

    // Runs the enemy's idle behaviour and transitions to roaming when the timer ends.
    private void PerformIdle()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= currentIdleDuration)
        {
            EnterRoamState();
        }
    }

    // Moves the enemy toward its roaming destination and returns to idle when it arrives.
    private void PerformRoam()
    {
        if (!roamPointSet)
        {
            EnterIdleState();
            return;
        }

        navAgent.SetDestination(currentRoamPoint);

        if (!navAgent.pathPending && navAgent.remainingDistance <= roamPointReachedDistance)
        {
            roamPointSet = false;
            EnterIdleState();
        }
    }

    // Moves the enemy toward the player's current position.
    private void PerformChase()
    {
        if (playerTransform != null)
        {
            navAgent.SetDestination(playerTransform.position);
        }
    }

    // Stops movement, faces the player, and performs attacks when off cooldown.
    private void PerformAttack()
    {
        navAgent.SetDestination(transform.position);

        if (playerTransform != null)
        {
            Vector3 lookTarget = new Vector3(
                playerTransform.position.x,
                transform.position.y,
                playerTransform.position.z
            );

            transform.LookAt(lookTarget);
        }

        if (!isOnAttackCooldown)
        {
            MeleeAttack();
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    // Runs the behaviour associated with the enemy's current active state.
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                PerformIdle();
                break;

            case EnemyState.Roam:
                PerformRoam();
                break;

            case EnemyState.Chase:
                PerformChase();
                break;

            case EnemyState.Attack:
                PerformAttack();
                break;
        }
    }

    // Determines when the enemy should transition between idle, roam, chase, and attack states.
    private void UpdateBehaviourState()
    {
        if (isPlayerVisible && isPlayerInRange)
        {
            if (currentState != EnemyState.Attack)
            {
                EnterAttackState();
            }

            return;
        }

        if (isPlayerVisible && !isPlayerInRange)
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChaseState();
            }

            return;
        }

        if (!isPlayerVisible && !isPlayerInRange)
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                EnterIdleState();
            }
        }
    }
}