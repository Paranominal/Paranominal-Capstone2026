using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EncounterEnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Paused,
        Aggro
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;

    [Header("Layers")]
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Properties")]
    [SerializeField] private float maxHealth = 20.0f;
    private float currentHealth;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float meleeDamage = 5f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private float attackWindup = 0.3f;

    private bool isOnAttackCooldown;
    private bool isAttacking;
    private bool isDying;
    private bool isCreatedBySpawner;
    private bool hasReportedDeathToSpawner;

    private EnemyState currentState;
    private EnemyEncounterManager ownerSpawner;

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

    // Places the enemy into its starting aggro state when the scene begins.
    private void Start()
    {
        EnterAggroState();
    }

    // Updates the enemy's active behaviour each frame.
    private void Update()
    {
        if (isDying)
        {
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        RunCurrentState();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Reports this enemy's death to its owning encounter manager if it has one.
    private void OnDestroy()
    {
        ReportDeathToSpawner();
    }

    // Stores a reference to the encounter manager that created this enemy.
    public void SetOwnerSpawner(EnemyEncounterManager spawner)
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

    // Sets whether the enemy should be paused or actively aggro towards the player.
    public void SetPaused(bool isPaused)
    {
        if (isDying)
        {
            return;
        }

        StopAllCoroutines();
        isAttacking = false;
        isOnAttackCooldown = false;

        if (isPaused)
        {
            EnterPausedState();
        }
        else
        {
            EnterAggroState();
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

        StopAllCoroutines();

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

    // Notifies the owning encounter manager that this enemy has died.
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

    // Draws the enemy's melee range and attack hit area in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
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

    // Handles the enemy's melee attack timing, hit frame, and cooldown.
    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;

        yield return new WaitForSeconds(attackWindup);

        if (isDying || currentState == EnemyState.Paused)
        {
            isAttacking = false;
            yield break;
        }

        MeleeAttack();
        StartCoroutine(AttackCooldownRoutine());

        isAttacking = false;
    }

    // Handles the delay between consecutive enemy attacks.
    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    // Switches the enemy into the paused state.
    private void EnterPausedState()
    {
        currentState = EnemyState.Paused;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
    }

    // Switches the enemy into the aggro state.
    private void EnterAggroState()
    {
        currentState = EnemyState.Aggro;

        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }
    }

    // Stops all movement and actions while the enemy is paused.
    private void PerformPaused()
    {
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
    }

    // Makes the enemy chase the player and attack when they are within melee range.
    private void PerformAggro()
    {
        if (playerTransform == null)
        {
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }

            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool isPlayerInMeleeRange = distanceToPlayer <= meleeRange;

        if (isPlayerInMeleeRange)
        {
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }

            Vector3 lookTarget = new Vector3(
                playerTransform.position.x,
                transform.position.y,
                playerTransform.position.z
            );

            transform.LookAt(lookTarget);

            if (!isAttacking && !isOnAttackCooldown)
            {
                StartCoroutine(MeleeAttackRoutine());
            }
        }
        else
        {
            if (navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(playerTransform.position);
            }
        }
    }

    // Runs the behaviour associated with the enemy's current active state.
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Paused:
                PerformPaused();
                break;

            case EnemyState.Aggro:
                PerformAggro();
                break;
        }
    }
}