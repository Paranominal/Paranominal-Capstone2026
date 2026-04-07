using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Roam,
        Chase,
        Search,
        Attack
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyVisionSensor vision;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Properties")]
    [SerializeField] private float maxHealth = 20.0f;
    private float currentHealth;

    [Header("Detection Timers")]
    [SerializeField] private float detectTimeToChase = 1.5f;
    [SerializeField] private float loseSightTimeToSearch = 2f;

    [Header("Roaming Settings")]
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float roamPointReachedDistance = 1f;

    [Header("Idle Settings")]
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 5f;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchTurnAngle = 45f;
    [SerializeField] private float searchTurnSpeed = 2f;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float meleeDamage = 5f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private float attackWindup = 0.3f;

    private bool isOnAttackCooldown;
    private bool isAttacking;
    private bool isDying;

    private bool isPlayerVisible;
    private bool isPlayerInMeleeRange;

    private EnemyState currentState;

    private Vector3 currentRoamPoint;
    private bool roamPointSet;

    private float idleTimer;
    private float currentIdleDuration;

    private float detectMeter;
    private float lostSightTimer;
    private float searchTimer;
    private float searchBaseYaw;

    private bool isCreatedBySpawner;
    private EnemyArenaSpawner ownerSpawner;
    private bool hasReportedDeathToSpawner;

    // Sets up component references and default values when the object is first loaded.
    private void Awake()
    {
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (attackPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name == "AttackPoint")
                {
                    attackPoint = child;
                    break;
                }
            }
        }

        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }

        if (vision == null)
        {
            vision = gameObject.AddComponent<EnemyVisionSensor>();
        }

        vision.AcquirePlayerTarget();

        if (playerTransform == null && vision.HasTarget)
        {
            playerTransform = vision.Target;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        currentHealth = maxHealth;
    }

    // Places the enemy into its starting idle state when the scene begins.
    private void Start()
    {
        if (isCreatedBySpawner)
        {
            // When a spawn animation is created, play it here.
        }

        EnterIdleState();
    }

    // Updates player detection, state transitions, and current state behaviour each frame.
    private void Update()
    {
        if (isDying)
        {
            return;
        }

        if (vision != null && !vision.HasTarget)
        {
            vision.AcquirePlayerTarget();
        }

        if (playerTransform == null && vision != null && vision.HasTarget)
        {
            playerTransform = vision.Target;
        }

        DetectPlayer();
        UpdateBehaviourState();
        RunCurrentState();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Reports this enemy's death to its owning spawner if it has one.
    private void OnDestroy()
    {
        ReportDeathToSpawner();
    }

    // Stores a reference to the spawner that created this enemy.
    public void SetOwnerSpawner(EnemyArenaSpawner spawner)
    {
        ownerSpawner = spawner;
        isCreatedBySpawner = true;
    }

    // Applies damage to the enemy and checks whether it should die.
    public void TakeDamage(float damageAmount)
    {
        if (isDying)
        {
            return;
        }

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Handles the enemy's death and cleanup.
    private void Die()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        if (isCreatedBySpawner)
        {
            ReportDeathToSpawner();
        }

        // When a death animation is created, play it here before destroying the object.
        Destroy(gameObject);
    }

    // Notifies the owning spawner that this enemy has died.
    private void ReportDeathToSpawner()
    {
        if (hasReportedDeathToSpawner)
        {
            return;
        }

        hasReportedDeathToSpawner = true;

        if (ownerSpawner != null)
        {
            ownerSpawner.NotifyEnemyDeath(this);
        }
    }

    // Draws the enemy's detection ranges, roam point, and melee hit area in the Scene view.
    private void OnDrawGizmosSelected()
    {
        if (vision != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, vision.viewDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, vision.ChaseDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, roamRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        if (roamPointSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentRoamPoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentRoamPoint);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }

    // Checks whether the player is visible via the vision sensor and whether they are close enough for melee.
    private void DetectPlayer()
    {
        if (vision == null)
        {
            isPlayerVisible = false;
            isPlayerInMeleeRange = false;
            return;
        }

        isPlayerVisible = vision.IsTargetInVision();

        float distanceToPlayer = vision.DistanceToTarget();
        isPlayerInMeleeRange = distanceToPlayer <= meleeRange;
    }

    // Performs the enemy's melee hit detection.
    private void MeleeAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{name} is missing an Attack Point reference.");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayerMask);

        foreach (Collider hitCollider in hitColliders)
        {
            if (playerTransform != null &&
                (hitCollider.transform == playerTransform || hitCollider.transform.IsChildOf(playerTransform)))
            {
                Debug.Log($"{name} landed a melee hit on {hitCollider.name} for {meleeDamage} damage.");
                return;
            }
        }

        Debug.Log($"{name} melee attack missed.");
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
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    currentRoamPoint = navHit.position;
                    roamPointSet = true;
                }
            }
        }
    }

    // Handles the enemy's melee attack timing, hit frame, and cooldown.
    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;

        yield return new WaitForSeconds(attackWindup);

        if (!isDying)
        {
            MeleeAttack();
            StartCoroutine(AttackCooldownRoutine());
        }

        isAttacking = false;
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

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        idleTimer = 0f;
        currentIdleDuration = Random.Range(idleTimeMin, idleTimeMax);
    }

    // Switches the enemy into the roam state and selects a new roam destination.
    private void EnterRoamState()
    {
        currentState = EnemyState.Roam;
        GenerateRoamPoint();

        if (roamPointSet && navAgent != null)
        {
            navAgent.isStopped = false;
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

        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }
    }

    // Switches the enemy into the search state.
    private void EnterSearchState()
    {
        currentState = EnemyState.Search;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        searchTimer = 0f;
        searchBaseYaw = transform.eulerAngles.y;
    }

    // Switches the enemy into the attack state.
    private void EnterAttackState()
    {
        currentState = EnemyState.Attack;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
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

        if (navAgent != null)
        {
            navAgent.SetDestination(currentRoamPoint);

            if (!navAgent.pathPending && navAgent.remainingDistance <= roamPointReachedDistance)
            {
                roamPointSet = false;
                EnterIdleState();
            }
        }
    }

    // Moves the enemy toward the player's current position.
    private void PerformChase()
    {
        if (playerTransform != null && navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(playerTransform.position);
        }
    }

    // Rotates in place while searching for the player, then returns to idle.
    private void PerformSearch()
    {
        searchTimer += Time.deltaTime;

        float yawOffset = Mathf.Sin(searchTimer * searchTurnSpeed) * searchTurnAngle;
        transform.rotation = Quaternion.Euler(0f, searchBaseYaw + yawOffset, 0f);

        if (searchTimer >= searchDuration)
        {
            EnterIdleState();
        }
    }

    // Stops movement, faces the player, and performs attacks when off cooldown.
    private void PerformAttack()
    {
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        if (playerTransform != null)
        {
            Vector3 lookTarget = new Vector3(
                playerTransform.position.x,
                transform.position.y,
                playerTransform.position.z
            );

            transform.LookAt(lookTarget);
        }

        if (!isAttacking && !isOnAttackCooldown)
        {
            StartCoroutine(MeleeAttackRoutine());
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

            case EnemyState.Search:
                PerformSearch();
                break;

            case EnemyState.Attack:
                PerformAttack();
                break;
        }
    }

    // Determines when the enemy should transition between idle, roam, chase, search, and attack.
    private void UpdateBehaviourState()
    {
        if (vision == null)
        {
            return;
        }

        float distanceToPlayer = vision.DistanceToTarget();
        bool withinChaseDistance = distanceToPlayer <= vision.ChaseDistance;

        if (isPlayerVisible)
        {
            detectMeter += Time.deltaTime;
            lostSightTimer = 0f;
        }
        else
        {
            detectMeter = Mathf.Max(0f, detectMeter - Time.deltaTime);

            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                lostSightTimer += Time.deltaTime;
            }
        }

        if (isPlayerVisible && isPlayerInMeleeRange)
        {
            if (currentState != EnemyState.Attack)
            {
                EnterAttackState();
            }

            return;
        }

        if (isPlayerVisible && (detectMeter >= detectTimeToChase || withinChaseDistance))
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChaseState();
            }

            return;
        }

        if ((currentState == EnemyState.Chase || currentState == EnemyState.Attack) &&
            !isPlayerVisible &&
            lostSightTimer >= loseSightTimeToSearch)
        {
            if (currentState != EnemyState.Search)
            {
                EnterSearchState();
            }

            return;
        }

        if (currentState == EnemyState.Search && isPlayerVisible)
        {
            EnterChaseState();
        }
    }
}